Public Class Zml
    Private AddComment As Boolean = True

    Const comment = "<!--This file is auto generated from the .zml file." + Ln +
                 "Make cahnges only to the .zml file, and don't make any changes here," + Ln +
                 "because they will be overwritten when the .zml file changes." + Ln +
                 "If you want to format this file to review some blocks," + Ln +
                 "use the Edit\Advanced\Format Document from main menus. -->"

    Dim CsCode As New List(Of String)
    Dim BlockStart, BlockEnd As String
    Dim Xml As XElement

    Public Sub New(Optional addComment As Boolean = True)
        Me.AddComment = addComment
    End Sub

    Function ParseZml(zml As XElement) As String
        BlockStart = AddToCsList("{")
        BlockEnd = AddToCsList("}")

        Xml = New XElement(zml)
        ParsePage()
        ParseModel()
        ParseViewData()
        ParseTitle()
        FixTagHelpers()
        ParseDeclarations()
        ParseSetters()
        ParseGetters()
        ParseIfStatements()
        ParseLoops()

        Dim x = Xml.ToString()
        For n = 0 To CsCode.Count - 1
            x = x.Replace($"<zmlitem{n} />", CsCode(n))
        Next

        x = x.Replace((TempRootStart, ""),
                 (TempRootEnd, ""),
                 (LessThan, "<"),
                 (GreaterThan, ">"),
                 (Ampersand, "&")
                 ).Trim(" ", vbCr, vbLf)

        Dim lines = x.Split(vbCrLf)
        Dim sb As New Text.StringBuilder()

        For Each line In lines
            If line = "  " Then
                sb.AppendLine("")
            ElseIf line.Trim <> "" Then
                If AddComment AndAlso Not line.Trim.StartsWith("@") Then
                    AddComment = False
                    sb.AppendLine(Ln + comment + Ln)
                End If
                If line.StartsWith("  ") Then
                    sb.AppendLine(line.Substring(2))
                Else
                    sb.AppendLine(line)
                End If
            End If
        Next
        Return sb.ToString().Trim(" ", vbCr, vbLf)
    End Function

    ' Qute string values, except objects (starting with @) and chars (quted by ' ')
    Private Function Quote(value As String) As String
        If value.StartsWith("@") Then
            Return value.Substring(1) ' value is object
        ElseIf value.StartsWith(SnglQt) AndAlso value.EndsWith(SnglQt) Then
            Return value ' value is char
        ElseIf value.StartsWith("#") AndAlso value.EndsWith("#") Then
            Return $"DateTime.Parse({Qt}{value.Trim("#")}{Qt})"
        ElseIf Double.TryParse(value, New Double()) Then
            Return value ' value is numeric       
        Else
            Return Qt + value.Trim(Qt) + Qt ' value is string
        End If
    End Function

    ' vars are always objects. Erase @ and qoutes
    Private Function At(value As String) As String
        If value.StartsWith("@") Then
            Return value.Substring(1)
        Else
            Return value.Trim(Qt).Trim(SnglQt)
        End If
    End Function

    Private Function GetXml(xml As String) As XElement
        Try
            Return XElement.Parse(xml)
        Catch
            Return XElement.Parse(TempRootStart + vbCrLf + xml + vbCrLf + TempRootEnd)
        End Try
    End Function

    Private Sub FixTagHelpers()
        Dim tageHelpers = From elm In Xml.Descendants()
                          From attr In elm.Attributes
                          Where attr.Name.LocalName.StartsWith("asp-")
                          Select attr

        For Each tageHelper In tageHelpers
            Dim value = tageHelper.Value
            If Not value.StartsWith("@") Then
                If value.StartsWith("Model") Then
                    tageHelper.Value = "@" + value
                Else
                    tageHelper.Value = "@Model." + value
                End If
            End If
        Next

    End Sub

    Private Sub ParseTitle()
        Dim viewTitle = (From elm In Xml.Descendants()
                         Where elm.Name = "viewtitle")?.FirstOrDefault

        If viewTitle IsNot Nothing Then
            Dim title = ""
            Dim value = If(viewTitle.Attribute("value")?.Value, viewTitle.Value)
            If value = "" Then 'Read Title
                title = $"@ViewData[{Qt }Title{Qt }]"
            Else ' Set Title
                title = "@{ " & $"ViewData[{Qt}Title{Qt}] = {Quote(value)};" & " }"
            End If

            title = AddToCsList(title)

            viewTitle.ReplaceWith(GetXml(title))

            ParseTitle()
        End If

    End Sub

    Private Function AddToCsList(item As String) As String
        CsCode.Add(item)
        Dim zmlKey = "zmlitem" & CsCode.Count - 1
        Return $"<{zmlKey}/>"
    End Function

    Private Sub ParseSetters()

        Dim setter = (From elm In Xml.Descendants()
                      Where elm.Name = "set").FirstOrDefault

        If setter IsNot Nothing Then
            Dim x = ""

            Dim obj = setter.Attribute("object")
            If obj Is Nothing Then
                ' Set multiple values

                Dim sb As New Text.StringBuilder(Ln + "@{" + Ln)
                For Each o In setter.Attributes
                    sb.AppendLine($"{  At(o.Name.ToString())} = {Quote(o.Value)};")
                Next
                sb.AppendLine("}" + Ln)
                x = sb.ToString()

            Else ' Set single value 
                Dim key = setter.Attribute("key")
                Dim value = Quote(If(setter.Attribute("value")?.Value, setter.Value))

                If key Is Nothing Then
                    ' Set single value without key
                    x = "@{ " + $"{At(obj.Value)} = {value};" + " }" + Ln

                Else ' Set single value with key
                    x = "@{ " + $"{At(obj.Value)}[{Quote(key.Value)}] = {value};" + " }" + Ln
                End If
            End If

            x = AddToCsList(x)
            setter.ReplaceWith(GetXml(x))
            ParseSetters()
        End If

    End Sub

    Private Sub ParseDeclarations()

        Dim dclr = (From elm In Xml.Descendants()
                    Where elm.Name = "declare").FirstOrDefault

        If dclr IsNot Nothing Then
            Dim x = ""
            Dim var = dclr.Attribute("var")

            If var Is Nothing Then
                ' Set multiple values

                Dim sb As New Text.StringBuilder(Ln + "@{" + Ln)
                For Each o In dclr.Attributes
                    sb.AppendLine($"  var {At(o.Name.ToString())} = {Quote(o.Value)};")
                Next
                sb.AppendLine("}" + Ln)
                x = sb.ToString()

            Else ' Set single value 
                Dim type = If(convVars(dclr.Attribute("type")?.Value), "var")
                Dim value = If(dclr.Attribute("value")?.Value, dclr.Value)
                Dim key = dclr.Attribute("key")

                If key Is Nothing Then
                    ' Set var value without key
                    x = "@{ " + $"{type} {At(var.Value)} = {Quote(value)};" + " }" + Ln

                Else ' Set single value with key
                    x = "@{ " + $"{type} {At(var.Value)} = {At(value)}[{Quote(key.Value)}];" + " }" + Ln
                End If
            End If

            x = AddToCsList(x)
            dclr.ReplaceWith(GetXml(x))
            ParseDeclarations()
        End If

    End Sub

    Private Function convVars(type As String) As String
        If type Is Nothing Then Return Nothing
        Dim t = type.Trim().ToLower()
        Select Case t
            Case "byte", "sbyte", "short", "ushort", "long", "ulong", "douple", "decimal"
                Return t
            Case "integer"
                Return "int"
            Case "uinteger"
                Return "uint"
            Case "single"
                Return "float"
            Case Else
                Return type.Trim().Replace(
                    (" Byte", " byte"), (" SBtye", " sbyte"), (" Short", " short"),
                    (" UShort", " ushort"), (" Long", " long"), (" ULong", " ulong"),
                    (" Double", " douple"), (" Decimal", " decimal"),
                    (" Integer", " int"), (" UInteger", " uint"), (" Single", " float"),
                    ("(Of ", LessThan), ("of ", LessThan), (")", GreaterThan)
                )

        End Select


    End Function

    Private Sub ParseGetters()

        Dim getter = (From elm In Xml.Descendants()
                      Where elm.Name = "get").FirstOrDefault

        If getter IsNot Nothing Then
            Dim key = getter.Attribute("key")
            Dim obj = At(If(getter.Attribute("object")?.Value, getter.Value))
            Dim x = ""

            If key Is Nothing Then
                x = $"@{obj}" + Ln
            Else
                x = $"@{obj}[{Quote(key.Value)}]" + Ln
            End If

            x = AddToCsList(x)
            getter.ReplaceWith(GetXml(x))

            ParseGetters()
        End If

    End Sub

    Private Sub ParseViewData()
        Dim viewdata = (From elm In Xml.Descendants()
                        Where elm.Name = "viewdata")?.FirstOrDefault

        If viewdata IsNot Nothing Then
            Dim keyAttr = viewdata.Attribute("key")
            Dim value = If(viewdata.Attribute("value")?.Value, viewdata.Value)

            If keyAttr Is Nothing Then
                ' Write miltiple values to ViewData
                ' <viewdata Name="'Ali'" Age="15"/>

                Dim sb As New Text.StringBuilder(Ln + "@{" + Ln)
                For Each key In viewdata.Attributes
                    sb.AppendLine($"ViewData[{At(key.Name.ToString())}] = {Quote(key.Value)};")
                Next
                sb.AppendLine("}" + Ln)

                Dim x = AddToCsList(sb.ToString())
                viewdata.ReplaceWith(GetXml(x))

            ElseIf value IsNot Nothing Then
                ' Write one value to ViewData
                ' <viewdata key="Age" value="15"/>
                ' or <viewdata key="Age">15</viewdata>

                Dim x = $"ViewData[{At(keyAttr.Value)}] = {Quote(value)};"
                x = AddToCsList(x)
                viewdata.ReplaceWith(GetXml(x))

            Else ' Read from ViewData
                ' <vewdata key="Age"/>

                Dim x = $"@ViewData[{At(keyAttr.Value)}]"
                x = AddToCsList(x)
                viewdata.ReplaceWith(GetXml(x))
            End If

            ParseViewData()
        End If

    End Sub

    Private Sub ParsePage()
        Dim page = (From elm In Xml.Descendants()
                    Where elm.Name = "page")?.FirstOrDefault

        If page IsNot Nothing Then
            Dim route = If(page.Attribute("route")?.Value, page.Value)
            Dim x = "@page "
            If route <> "" Then x += Qt + route + Qt

            x = AddToCsList(x)
            page.ReplaceWith(GetXml(x))
        End If
    End Sub

    Private Sub ParseModel()
        Dim model = (From elm In Xml.Descendants()
                     Where elm.Name = "model")?.FirstOrDefault

        If model IsNot Nothing Then
            Dim type = If(model.Attribute("type")?.Value, model.Value)
            Dim x = "@model " + type.
                Replace(("(Of ", LessThan), ("of ", LessThan),
                 (")", GreaterThan))

            x = AddToCsList(x)
            model.ReplaceWith(GetXml(x))
        End If
    End Sub

    Private Sub ParseLoops()
        Dim foreach = (From elm In Xml.Descendants()
                       Where elm.Name = "foreach")?.FirstOrDefault

        If foreach IsNot Nothing Then
            Dim _var = At(foreach.Attribute("var").Value)
            Dim _in = foreach.Attribute("in").Value.Replace("@Model.", "Model.")
            Dim st = $"@foreach (var {_var} in {_in})"
            st = AddToCsList(st)
            Dim x = st + Ln +
                         BlockStart +
                         foreach.InnerXML +
                         BlockEnd

            foreach.ReplaceWith(GetXml(x))

            ParseLoops()
        End If
    End Sub

    Private Sub ParseIfStatements()
        Dim _if = (From elm In Xml.Descendants()
                   Where elm.Name = "if")?.FirstOrDefault

        If _if IsNot Nothing Then
            Dim st = ""

            If _if.Nodes.Count > 0 Then
                Dim children = _if.Nodes
                Dim firstChild As XElement = children(0)
                If firstChild.Name = "then" Then
                    st = "@if (" + ConvLog(_if.Attribute("condition").Value) + ")"
                    st = AddToCsList(st)

                    Dim _then = st + Ln +
                            BlockStart + firstChild.InnerXML + BlockEnd

                    Dim _elseifs = ParseElseIfs(_if)

                    Dim _else = ""
                    Dim lastChild As XElement = children(children.Count - 1)
                    If lastChild.Name = "else" Then
                        st = AddToCsList("else")
                        _else = st + Ln +
                                    BlockStart + lastChild.InnerXML + BlockEnd

                    End If

                    _if.ReplaceWith(GetXml(_then + _elseifs + _else))

                Else
                    st = "@if (" + ConvLog(_if.Attribute("condition").Value) + ")"
                    st = AddToCsList(st)
                    Dim x = st + Ln +
                                  BlockStart + firstChild.ToString() + BlockEnd

                    _if.ReplaceWith(GetXml(x))
                End If
            End If

            ParseIfStatements()
        End If

    End Sub

    Private Function ConvLog(value As String) As String
        Dim x = value.Replace(
            ("@Model.", "Model."),
            (" And ", $" {Ampersand} "), (" and ", $" {Ampersand} "),
            (" AndAlso ", $" {Ampersand + Ampersand} "), (" andalso ", $" {Ampersand + Ampersand} "),
            (" Or ", " | "), (" or ", " | "), (" mod ", " % "), (" Mod ", " % "),
            (" OrElse ", " || "), (" orelse ", " || "),
            (" Not ", " !"), (" not ", " !"),
            (" Xor ", " ^ "), (" xor ", " ^ "),
            (" <> ", " != "), (" = ", " == "), ("====", "=="),
            (" IsNot ", " != "), (" isnot ", " != "),
            (">", GreaterThan), (">", LessThan))

        Return x

    End Function

    Private Function ParseElseIfs(ifXml As XElement) As String
        Dim _elseifs = (From elm In ifXml.Descendants()
                        Where elm.Name = "elseif")

        Dim sb As New Text.StringBuilder()
        Dim x = ""
        Dim st = ""

        For Each _elseif In _elseifs
            st = "else if (" + ConvLog(_elseif.Attribute("condition").Value) + ")"
            st = AddToCsList(st)
            x = st + Ln +
                BlockStart + _elseif.InnerXML + BlockEnd
            sb.AppendLine(x)
        Next
        Return sb.ToString()
    End Function

End Class
