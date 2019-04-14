Public Class Zml
    Private AddComment As Boolean = True

    Const comment = "<!--This file is auto generated from the .zml file." + Ln +
                 "Make cahnges only to the .zml file, and don't make any changes here," + Ln +
                 "because they will be overwritten when the .zml file changes." + Ln +
                 "If you want to format this file to review some blocks," + Ln +
                 "use the Edit\Advanced\Format Document from main menus. -->"

    Dim CsCode As New List(Of String)
    Dim BlockStart, BlockEnd As String

    Public Sub New(Optional addComment As Boolean = True)
        Me.AddComment = addComment
    End Sub

    Function ParseZml(zml As XElement) As String
        BlockStart = AddToCsList("{")
        BlockEnd = AddToCsList("}")

        Dim xml = New XElement(zml)
        ParsePage(xml)
        ParseModel(xml)
        ParseViewData(xml)
        ParseTitle(xml)
        FixTagHelpers(xml)
        PsrseSetters(xml)
        PsrseGetters(xml)
        PsrseConditions(xml)
        PsrseLoops(xml)

        Dim sb As New Text.StringBuilder(xml.ToString())

        For n = 0 To CsCode.Count - 1
            sb.Replace($"<zmlitem{n} />", CsCode(n))
        Next

        sb.Replace(
                            (TempRootStart, ""), (TempRootEnd, ""),
                            (LessThan, "<"), (GreaterThan, ">"),
                            (Ampersand, "&")
                         )

        Dim x = sb.ToString().Trim(" ", vbCr, vbLf)
        sb.Clear()
        Dim lines = x.Split(vbCrLf)

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

    Private Sub FixTagHelpers(xml As XElement)
        Dim tageHelpers = From elm In xml.Descendants()
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

    Private Sub ParseTitle(xml As XElement)
        Dim viewTitle = (From elm In xml.Descendants()
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

            ParseTitle(xml)
        End If

    End Sub

    Private Function AddToCsList(item As String) As String
        CsCode.Add(item)
        Dim zmlKey = "zmlitem" & CsCode.Count - 1
        Return $"<{zmlKey}/>"
    End Function

    Private Sub PsrseSetters(xml As XElement)

        Dim setter = (From elm In xml.Descendants()
                      Where elm.Name = "set").FirstOrDefault

        If setter IsNot Nothing Then
            Dim x = ""

            Dim obj = setter.Attribute("object")
            If obj Is Nothing Then
                ' Set multiple values

                Dim sb As New Text.StringBuilder(Ln + "@{" + Ln)
                For Each o In setter.Attributes
                    sb.AppendLine($"{At(o.Name.ToString())} = {Quote(o.Value)};")
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
            PsrseSetters(xml)
        End If

    End Sub

    Private Sub PsrseGetters(xml As XElement)

        Dim getter = (From elm In xml.Descendants()
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

            PsrseGetters(xml)
        End If

    End Sub

    Private Sub ParseViewData(xml As XElement)
        Dim viewdata = (From elm In xml.Descendants()
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

            ParseViewData(xml)
        End If

    End Sub

    Private Sub ParsePage(xml As XElement)
        Dim page = (From elm In xml.Descendants()
                    Where elm.Name = "page")?.FirstOrDefault

        If page IsNot Nothing Then
            Dim route = If(page.Attribute("route")?.Value, page.Value)
            Dim x = "@page "
            If route <> "" Then x += Qt + route + Qt

            x = AddToCsList(x)
            page.ReplaceWith(GetXml(x))
        End If
    End Sub

    Private Sub ParseModel(xml As XElement)
        Dim model = (From elm In xml.Descendants()
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

    Private Sub PsrseLoops(xml As XElement)
        Dim foreach = (From elm In xml.Descendants()
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

            PsrseLoops(xml)
        End If
    End Sub

    Private Sub PsrseConditions(xml As XElement)
        Dim _if = (From elm In xml.Descendants()
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

                    Dim _elseifs = PsrseElseIfs(_if)

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

            PsrseConditions(xml)
        End If

    End Sub

    Private Function ConvLog(value As String) As String
        Return value.Replace(
            ("@Model.", "Model."),
            (" And ", $" {Ampersand} "), (" and ", $" {Ampersand} "),
            (" AndAlso ", $" {Ampersand + Ampersand} "), (" andalso ", $" {Ampersand + Ampersand} "),
            (" Or ", " | "), (" or ", " | "),
            (" OrElse ", " || "), (" orelse ", " || "),
            (" Not ", " !"), (" not ", " !"),
            (" Xor ", " ^ "), (" xor ", " ^ "),
            (" = ", " == "), (" <> ", " != "),
            (" IsNot ", " != "), (" isnot ", " != "),
            (">", GreaterThan), (">", LessThan))
    End Function

    Private Function PsrseElseIfs(xml As XElement) As String
        Dim _elseifs = (From elm In xml.Descendants()
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
