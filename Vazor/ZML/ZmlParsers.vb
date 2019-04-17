Partial Public Class Zml

    Function ParseZml(zml As XElement) As String
        BlockStart = AddToCsList("{")
        BlockEnd = AddToCsList("}")

        Xml = New XElement(zml)
        ParseImports()
        ParseHelperImports()
        ParseLayout()
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
                 (ChngQt, ""), (SnglQt + SnglQt, Qt)
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

    Private Sub FixTagHelpers()
        Dim tageHelpers = From elm In Xml.Descendants()
                          From attr In elm.Attributes
                          Where attr.Name.LocalName.StartsWith(aspHelperPrefix)
                          Select attr

        For Each tageHelper In tageHelpers
            Dim value = tageHelper.Value
            If Not value.StartsWith("@") Then
                If value.StartsWith(modelKeyword) Then
                    tageHelper.Value = "@" + value
                ElseIf Not tageHelper.Name.ToString.Contains(pageHelperPrefix) Then
                    tageHelper.Value = atModel + "." + value
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
                attr.Value = ChngQt & value & ChngQt
            End If
        Next

    End Sub

    Private Sub ParseTitle()
        Dim viewTitle = (From elm In Xml.Descendants()
                         Where elm.Name = viewtitleTag)?.FirstOrDefault

        If viewTitle IsNot Nothing Then
            Dim title = ""
            Dim value = If(viewTitle.Attribute(valueAttr)?.Value, viewTitle.Value)
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
                       Where elm.Name = commentTag).FirstOrDefault

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
                      Where elm.Name = setTag).FirstOrDefault

        If setter IsNot Nothing Then
            Dim x = ""

            Dim obj = setter.Attribute(objectAttr)
            If obj Is Nothing Then
                ' Set multiple values

                Dim sb As New Text.StringBuilder(Ln + "@{" + Ln)
                For Each o In setter.Attributes
                    sb.AppendLine($"{At(o.Name.ToString())} = {Quote(o.Value)};")
                Next
                sb.AppendLine("}" + Ln)
                x = sb.ToString()

            Else ' Set single value 
                Dim key = setter.Attribute(keyAttr)
                Dim value = Quote(If(setter.Attribute(valueAttr)?.Value, setter.Value))

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
                        Where elm.Name = declareTag).FirstOrDefault

        If _declare IsNot Nothing Then
            Dim x = ""
            Dim var = _declare.Attribute(varAttr)

            Dim insideCsBlock = IsInsideCsBlock(_declare)

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
                Dim type = If(convVars(_declare.Attribute(typeAttr)?.Value), varAttr)
                Dim value = If(_declare.Attribute(valueAttr)?.Value, _declare.Value)
                Dim key = _declare.Attribute(keyAttr)

                Dim blkSt = ""
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

    Private Sub ParseGetters()

        Dim getter = (From elm In Xml.Descendants()
                      Where elm.Name = getTag).FirstOrDefault

        If getter IsNot Nothing Then
            Dim key = getter.Attribute(keyAttr)
            Dim obj = At(If(getter.Attribute(objectAttr)?.Value, getter.Value))
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
            Dim _keyAttr = viewdata.Attribute(keyAttr)
            Dim value = If(viewdata.Attribute(valueAttr)?.Value, viewdata.Value)

            If _keyAttr Is Nothing Then
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

                Dim x = $"ViewData[{At(_keyAttr.Value)}] = {Quote(value)};"
                viewdata.ReplaceWith(AddToCsList(x))

            Else ' Read from ViewData
                Dim x = $"@ViewData[{At(_keyAttr.Value)}]"
                viewdata.ReplaceWith(AddToCsList(x))
            End If

            ParseViewData()
        End If

    End Sub

    Private Sub ParseImports()
        Dim tag = (From elm In Xml.Descendants()
                   Where elm.Name = importsTag OrElse
                            elm.Name = usingTag OrElse
                            elm.Name = namespaceTag)?.FirstOrDefault

        If tag IsNot Nothing Then
            Dim ns = If(tag.Attribute(nsAttr)?.Value, tag.Value)
            Dim _using
            Dim x = ""

            If tag.Name.ToString = namespaceTag Then
                _using = AtNamespace & " "
            Else
                _using = AtUsing & " "
            End If

            If ns = "" Then
                Dim sb As New Text.StringBuilder
                For Each attr In tag.Attributes
                    sb.AppendLine(_using + attr.Name.ToString())
                Next
                x = sb.ToString()
            Else
                x = _using + ns
            End If

            tag.ReplaceWith(AddToCsList(x))
            ParseImports()
        End If
    End Sub

    Private Sub ParseHelperImports()
        Dim helper = (From elm In Xml.Descendants()
                      Where elm.Name = helpersTag)?.FirstOrDefault

        If helper IsNot Nothing Then
            Dim ns = If(helper.Attribute(nsAttr)?.Value, helper.Value)
            Dim x = ""
            If ns = "" Then
                Dim sb As New Text.StringBuilder
                For Each attr In helper.Attributes
                    sb.AppendLine($"@addTagHelper {attr.Value}, {attr.Name}")
                Next
                x = sb.ToString()
            Else
                Dim add = If(helper.Attribute(addAttr)?.Value, "*")
                x = $"@addTagHelper {add}, {ns}"
            End If

            helper.ReplaceWith(AddToCsList(x))
            ParseHelperImports()
        End If
    End Sub

    Private Sub ParsePage()
        Dim page = (From elm In Xml.Descendants()
                    Where elm.Name = pageTag)?.FirstOrDefault

        If page IsNot Nothing Then
            Dim route = If(page.Attribute(routeAttr)?.Value, page.Value)
            Dim x = AtPage & " "
            If route <> "" Then x += Qt + route + Qt

            page.ReplaceWith(AddToCsList(x))
            ParsePage()
        End If
    End Sub

    Private Sub ParseLayout()
        Dim layout = (From elm In Xml.Descendants()
                      Where elm.Name = layoutTag)?.FirstOrDefault

        If layout IsNot Nothing Then
            Dim page = If(layout.Attribute(pageAttr)?.Value, layout.Value)
            Dim x = "@{" + Ln + $"      Layout = {Qt}{page}{Qt};" + Ln + "}"

            layout.ReplaceWith(AddToCsList(x))
        End If
    End Sub


    Private Sub ParseModel()
        Dim model = (From elm In Xml.Descendants()
                     Where elm.Name = modelTag)?.FirstOrDefault

        If model IsNot Nothing Then
            Dim type = If(model.Attribute(typeAttr)?.Value, model.Value)
            Dim x = "@model " + convVars(type)

            model.ReplaceWith(AddToCsList(x))
        End If
    End Sub

    Private Sub ParseForLoops()
        Dim _for = (From elm In Xml.Descendants()
                    Where elm.Name = forTag)?.FirstOrDefault

        If _for IsNot Nothing Then
            Dim var, _to, _step, _while, _let, type As XAttribute

            For Each attr In _for.Attributes
                Select Case attr.Name.ToString().ToLower()
                    Case toAttr
                        _to = attr
                    Case stepAttr
                        _step = attr
                    Case whileattr
                        _while = attr
                    Case letAttr
                        _let = attr
                    Case typeAttr
                        type = attr
                    Case Else
                        var = attr
                End Select
            Next

            Dim varName = At(var.Name.ToString())
            Dim cond = ""
            Dim inc = ""

            If _to IsNot Nothing Then
                cond = _to.Value.Trim().Replace(atModel + ".", modelKeyword + ".")
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
                ElseIf cond.EndsWith(" > 0 - 1") Then
                    cond = cond.Substring(0, cond.Length - 5) & "-1"
                End If
                Else
                cond = _while.Value.Replace(atModel + ".", ModelKeyword + ".")
                inc = _let.Value.Replace(atModel + ".", ModelKeyword + ".")
            End If

            Dim typeName = If(convVars(type?.Value), varAttr)

            Dim st = $"@for ({typeName} {varName} = {Quote(var.Value)}; {cond}; {inc})"

            _for.ReplaceWith(GetCsHtml(st, _for))

            ParseForLoops()
        End If
    End Sub

    Private Sub ParseForEachLoops()
        Dim foreach = (From elm In Xml.Descendants()
                       Where elm.Name = foreachTag)?.FirstOrDefault

        If foreach IsNot Nothing Then
            Dim type = At(If(convVars(foreach.Attribute(typeAttr)?.Value), "var"))
            Dim _var = At(foreach.Attribute(varAttr)?.Value)
            Dim _in = foreach.Attribute(inAttr).Value.Replace(atModel + ".", ModelKeyword + ".")

            If _var = "" Then
                _var = (From attr In foreach.Attributes
                        Where attr.Value = "").FirstOrDefault.Name.ToString()
            End If

            Dim st = $"@foreach ({type} {_var} in {_in})"

            foreach.ReplaceWith(GetCsHtml(st, foreach))

            ParseForEachLoops()
        End If
    End Sub

    Private Sub ParseIfStatements()
        Dim _if = (From elm In Xml.Descendants()
                   Where elm.Name = ifTag)?.FirstOrDefault

        If _if IsNot Nothing Then

            If _if.Nodes.Count > 0 Then
                Dim children = _if.Nodes
                Dim firstChild As XElement = children(0)
                If firstChild.Name = thenTag Then
                    Dim cs = "@if (" + ConvLog(_if.Attribute(conditionAttr).Value) + ")"
                    Dim _then = GetCsHtml(cs, firstChild)

                    Dim _elseifs = ParseElseIfs(_if)

                    Dim _else As XElement
                    Dim lastChild As XElement = children(children.Count - 1)
                    If lastChild.Name = elseTag Then
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


    Private Function ParseElseIfs(ifXml As XElement) As List(Of XElement)
        Dim _elseifs = (From elm In ifXml.Descendants()
                        Where elm.Name = elseifTag)

        Dim x As New List(Of XElement)
        Dim cs = ""

        For Each _elseif In _elseifs
            cs = "else if (" + ConvLog(_elseif.Attribute(conditionAttr).Value) + ")"
            x.Add(GetCsHtml(cs, _elseif))
        Next

        Return x
    End Function

End Class
