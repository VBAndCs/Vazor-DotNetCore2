Public Class Zml
    Private AddComment As Boolean = True

    Const comment = "@*<!--This file is auto generated from the .zml file." + Ln +
                 "Make cahnges only to the .zml file, and don't make any changes here," + Ln +
                 "because they will be overwritten when the .zml file changes." + Ln +
                 "If you want to format this file to review some blocks," + Ln +
                 "use the Edit\Advanced\Format Document from main menus. -->*@"

    Dim CsCode As New List(Of String)
    Dim BlockStart, BlockEnd As XElement
    Dim Xml As XElement
    Const TempBodyStart = "<zmlbody>"
    Const TempBodyEnd = "</zmlbody>"
    Const ChngQt = "__chngqt__"
    Const tempDblQt = "__dblqt__"

    Private Function AddToCsList(item As String) As XElement
        CsCode.Add(item)
        Dim zmlKey = "zmlitem" & CsCode.Count - 1
        Return <<%= zmlKey %>/>
    End Function

    Public Sub New(Optional addComment As Boolean = True)
        Me.AddComment = addComment
    End Sub

    Function ParseZml(zml As XElement) As String
        BlockStart = AddToCsList("{")
        BlockEnd = AddToCsList("}")

        Xml = New XElement(zml)
        ParsePage()
        ParseModel()
        ParseTitle()
        FixTagHelpers()
        FixAttrExpressions()
        ParseIfStatements()
        ParseForEachLoops()
        ParseForLoops()
        ParseComments()
        ParseViewData()
        ParseSetters()
        ParseGetters()
        ParseDeclarations()

        Dim x = Xml.ToString()
        For n = 0 To CsCode.Count - 1
            x = x.Replace($"<zmlitem{n} />", CsCode(n))
        Next

        x = x.Replace(
                 (LessThan, "<"), (GreaterThan, ">"),
                 (Ampersand, "&"),
                 (Qt + ChngQt, SnglQt),
                 (ChngQt + Qt, SnglQt),
                 (tempDblQt, Qt)
                 ).Trim(" ", vbCr, vbLf)

        Dim lines = x.Split(vbCrLf)
        Dim sb As New Text.StringBuilder()

        Dim offset = 0

        For Each line In lines
            Dim absLine = line.Trim()
            If line = "  " Then
                sb.AppendLine("")
            ElseIf absLine = TempRootStart Then
                offset += 2
            ElseIf absLine = TempRootEnd Then
                offset -= 2
            ElseIf absLine <> "" AndAlso absLine <> TempBodyStart AndAlso absLine <> TempBodyEnd Then
                If AddComment AndAlso Not absLine.StartsWith("@") Then
                    AddComment = False
                    sb.AppendLine(Ln + comment + Ln)
                End If

                line = line.Replace((TempBodyStart, ""), (TempBodyEnd, ""))
                If line.Length > offset AndAlso line.StartsWith(New String(" ", offset)) Then
                    sb.AppendLine(line.Substring(offset))
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

    Private Function CombineXml(ParamArray xml() As XElement) As XElement
        Dim x = <zml/>
        x.Add(xml)
        Return x
    End Function

    Private Function CombineXml(header As XElement, blocks As List(Of XElement), footer As XElement) As XElement
        Dim x = <zml/>
        x.Add(header)
        x.Add(blocks)
        x.Add(footer)
        Return x
    End Function

    Private Function GetCsHtml(cs As String, html As XElement, Optional UseInner As Boolean = True) As XElement
        Dim x = <zml/>
        x.Add(
                    AddToCsList(cs),
                     BlockStart,
                        If(UseInner, html.InnerXml, html.OuterXml),
                     BlockEnd
                  )
        Return x
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
                ElseIf Not tageHelper.Name.ToString.Contains("-page") Then
                    tageHelper.Value = "@Model." + value
                End If
            End If
        Next

    End Sub

    Sub FixAttrExpressions()
        Dim attrs = From elm In Xml.Descendants()
                    From attr In elm.Attributes
                    Select attr

        For Each attr In attrs
            Dim value = attr.Value
            If value.Contains(SnglQt & SnglQt) Then
                attr.Value = ChngQt & value.Replace(SnglQt & SnglQt, tempDblQt) & ChngQt
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

            viewTitle.ReplaceWith(AddToCsList(title))

            ParseTitle()
        End If

    End Sub


    Private Sub ParseComments()
        Dim comment = (From elm In Xml.Descendants()
                       Where elm.Name = "comment").FirstOrDefault

        If comment IsNot Nothing Then
            Dim x = CombineXml(
                      AddToCsList("@*"),
                      comment.InnerXml,
                     AddToCsList("*@"))

            comment.ReplaceWith(x)
            ParseComments()
        End If
    End Sub

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

            setter.ReplaceWith(AddToCsList(x))
            ParseSetters()
        End If

    End Sub

    Private Sub ParseDeclarations()

        Dim _declare = (From elm In Xml.Descendants()
                        Where elm.Name = "declare").FirstOrDefault

        If _declare IsNot Nothing Then
            Dim x = ""
            Dim var = _declare.Attribute("var")

            Dim insideCsBlock = False
            Dim pn As XElement = _declare.PreviousNode
            Dim blkSt = BlockStart.Name.ToString()

            Do While PN IsNot Nothing
                If pn.Name.ToString() = blkSt Then
                    insideCsBlock = True
                    Exit Do
                End If
            Loop

            If var Is Nothing Then
                ' Set multiple values

                Dim sb As New Text.StringBuilder()
                If Not insideCsBlock Then sb.AppendLine("@{" + Ln)

                For Each o In _declare.Attributes
                    sb.AppendLine($"  var {At(o.Name.ToString())} = {Quote(o.Value)};")
                Next

                If Not insideCsBlock Then sb.AppendLine("}")
                x = sb.ToString()

            Else ' Set single value 
                Dim type = If(convVars(_declare.Attribute("type")?.Value), "var")
                Dim value = If(_declare.Attribute("value")?.Value, _declare.Value)
                Dim key = _declare.Attribute("key")

                blkSt = ""
                Dim blkEnd = ""
                If Not insideCsBlock Then
                    blkSt = "@{ "
                    blkEnd = " }"
                End If

                If key Is Nothing Then
                    ' Set var value without key
                    x = blkSt + $"{type} {At(var.Value)} = {Quote(value)};" + blkEnd

                Else ' Set single value with key
                    x = blkSt + $"{type} {At(var.Value)} = {At(value)}[{Quote(key.Value)}];" + blkEnd
                End If
            End If

            _declare.ReplaceWith(AddToCsList(x))
            ParseDeclarations()
        End If

    End Sub

    Private Function convVars(type As String) As String
        If type Is Nothing Then Return Nothing
        Dim t = type.Trim().ToLower()
        Select Case t
            Case "byte", "sbyte", "short", "ushort", "long", "ulong", "double", "decimal"
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
                    (" Double", " double"), (" Decimal", " decimal"),
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

            getter.ReplaceWith(AddToCsList(x))

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

                viewdata.ReplaceWith(AddToCsList(sb.ToString()))

            ElseIf value IsNot Nothing Then
                ' Write one value to ViewData
                ' <viewdata key="Age" value="15"/>
                ' or <viewdata key="Age">15</viewdata>

                Dim x = $"ViewData[{At(keyAttr.Value)}] = {Quote(value)};"
                viewdata.ReplaceWith(AddToCsList(x))

            Else ' Read from ViewData
                ' <vewdata key="Age"/>

                Dim x = $"@ViewData[{At(keyAttr.Value)}]"
                viewdata.ReplaceWith(AddToCsList(x))
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

            page.ReplaceWith(AddToCsList(x))
        End If
    End Sub

    Private Sub ParseModel()
        Dim model = (From elm In Xml.Descendants()
                     Where elm.Name = "model")?.FirstOrDefault

        If model IsNot Nothing Then
            Dim type = If(model.Attribute("type")?.Value, model.Value)
            Dim x = "@model " + convVars(type)

            model.ReplaceWith(AddToCsList(x))
        End If
    End Sub

    Private Sub ParseForLoops()
        Dim _for = (From elm In Xml.Descendants()
                    Where elm.Name = "for")?.FirstOrDefault

        If _for IsNot Nothing Then
            Dim var, _to, _step, _while, _let, type As XAttribute

            For Each attr In _for.Attributes
                Select Case attr.Name.ToString().ToLower()
                    Case "to"
                        _to = attr
                    Case "step"
                        _step = attr
                    Case "while"
                        _while = attr
                    Case "let"
                        _let = attr
                    Case "type"
                        type = attr
                    Case Else
                        var = attr
                End Select
            Next

            Dim varName = At(var.Name.ToString())
            Dim cond = ""
            Dim inc = ""

            If _to IsNot Nothing Then
                cond = _to.Value.Trim().Replace("@Model.", "Model.")
                Dim n As Integer
                Dim L = cond.Length - 1
                Dim reverseLoop = False

                If _step Is Nothing Then
                    inc = varName + "++"
                Else
                    Dim value = Quote(_step.Value)
                    If Integer.TryParse(value, n) Then
                        If n > 0 Then
                            inc = varName & " += " & n
                        ElseIf n = -1 Then
                            inc = varName & "--"
                            reverseLoop = True
                        Else
                            inc = varName & " -= " & Math.Abs(n)
                            reverseLoop = True
                        End If
                    Else
                        inc = varName & " += " & value
                    End If

                End If

                cond = varName + If(reverseLoop, " > " & cond & " - 1", " < " & cond & " + 1")
                If cond.EndsWithAny(" - 1 + 1", " + 1 - 1") Then
                    cond = cond.Substring(0, cond.Length - 8)
                End If
            Else
                cond = _while.Value.Replace("@Model.", "Model.")
                inc = _let.Value.Replace("@Model.", "Model.")
            End If

            Dim typeName = If(convVars(type?.Value), "var")

            Dim st = $"@for ({typeName} {varName} = {Quote(var.Value)}; {cond}; {inc})"

            _for.ReplaceWith(GetCsHtml(st, _for))

            ParseForLoops()
        End If
    End Sub

    Private Sub ParseForEachLoops()
        Dim foreach = (From elm In Xml.Descendants()
                       Where elm.Name = "foreach")?.FirstOrDefault

        If foreach IsNot Nothing Then
            Dim _var = At(foreach.Attribute("var").Value)
            Dim _in = foreach.Attribute("in").Value.Replace("@Model.", "Model.")
            Dim st = $"@foreach (var {_var} in {_in})"

            foreach.ReplaceWith(GetCsHtml(st, foreach))

            ParseForEachLoops()
        End If
    End Sub

    Private Sub ParseIfStatements()
        Dim _if = (From elm In Xml.Descendants()
                   Where elm.Name = "if")?.FirstOrDefault

        If _if IsNot Nothing Then

            If _if.Nodes.Count > 0 Then
                Dim children = _if.Nodes
                Dim firstChild As XElement = children(0)
                If firstChild.Name = "then" Then
                    Dim cs = "@if (" + ConvLog(_if.Attribute("condition").Value) + ")"
                    Dim _then = GetCsHtml(cs, firstChild)

                    Dim _elseifs = ParseElseIfs(_if)

                    Dim _else As XElement
                    Dim lastChild As XElement = children(children.Count - 1)
                    If lastChild.Name = "else" Then
                        _else = GetCsHtml("else", lastChild)
                    End If

                    If _elseifs.Count = 0 Then
                        _if.ReplaceWith(CombineXml(_then, _else))
                    Else
                        _if.ReplaceWith(CombineXml(_then, _elseifs, _else))
                    End If
                Else
                    Dim cs = "@if (" + ConvLog(_if.Attribute("condition").Value) + ")"
                    _if.ReplaceWith(GetCsHtml(cs, firstChild, False))
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

    Private Function ParseElseIfs(ifXml As XElement) As List(Of XElement)
        Dim _elseifs = (From elm In ifXml.Descendants()
                        Where elm.Name = "elseif")

        Dim x As New List(Of XElement)
        Dim cs = ""

        For Each _elseif In _elseifs
            cs = "else if (" + ConvLog(_elseif.Attribute("condition").Value) + ")"
            x.Add(GetCsHtml(cs, _elseif))
        Next

        Return x
    End Function

End Class
