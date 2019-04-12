' ZML Parser: Converts ZML tags to C# Razor statements.
' Copyright (c) 2019 Mohammad Hamdy Ghanem


Imports System.Reflection
Imports System.Runtime.CompilerServices

Public Module ZML


    Private Function GetXml(xml As String) As XElement
        Return XElement.Parse("<zml>" + xml + "</zml>")
    End Function

    <Extension>
    Public Function InnerXML(el As XElement) As String
        Dim reader = el.CreateReader()
        reader.MoveToContent()
        Return reader.ReadInnerXml()
    End Function

    Friend Function ParseZml(zml As String) As String
        Return GetXml(zml).ParseZml()
    End Function

    <Extension>
    Function ParseZml(xml As XElement) As String
        ParsePage(xml)
        ParseModel(xml)
        ParseViewData(xml)
        ParseTitle(xml)
        PsrseSetters(xml)
        PsrseGetters(xml)
        PsrseConditions(xml)
        PsrseLoops(xml)
        FixTagHelpers(xml)

        Return xml.ToString(SaveOptions.DisableFormatting).
            Replace("<zml>", "").Replace("</zml>", "").
            Replace("__ltn__", "<").Replace("__gtn__", ">").
            Replace("__amp__", "&")
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
            If viewTitle.Value = "" Then
                title = $"@ViewData['Title']"
            Else
                title = "@{ " & $"ViewData['Title'] = '{viewTitle.Value}';" & " }"
            End If

            viewTitle.ReplaceWith(GetXml(title.Replace("'", """")))

            ParseTitle(xml)
        End If

    End Sub

    Private Sub ParseViewData(xml As XElement)
        Dim viewdata = (From elm In xml.Descendants()
                        Where elm.Name = "viewdata")?.FirstOrDefault

        If viewdata IsNot Nothing Then
            Dim getKey = viewdata.Attribute("key")
            If getKey Is Nothing Then
                Dim sb As New Text.StringBuilder(vbCrLf + "@{" + vbCrLf)
                For Each key In viewdata.Attributes
                    sb.AppendLine($"ViewData['{key.Name}'] = '{key.Value}';")
                Next
                sb.AppendLine("}" + vbCrLf)
                viewdata.ReplaceWith(GetXml(sb.ToString().Replace("'", """").Trim()))
            Else
                Dim x = $"@ViewData['{getKey.Value }']".Replace("'", """")
                viewdata.ReplaceWith(GetXml(x))
            End If

            ParseViewData(xml)
        End If

    End Sub

    Private Sub ParsePage(xml As XElement)
        Dim page = (From elm In xml.Descendants()
                    Where elm.Name = "page")?.FirstOrDefault

        If page IsNot Nothing Then
            Dim x = "@page" + vbCrLf
            page.ReplaceWith(GetXml(x))
        End If
    End Sub

    Private Sub ParseModel(xml As XElement)
        Dim model = (From elm In xml.Descendants()
                     Where elm.Name = "model")?.FirstOrDefault

        If model IsNot Nothing Then
            Dim x = "@model "
            Dim type = model.Attribute("type")
            If Type Is Nothing Then
                x += $"{model.Value}"
            Else
                x += $"{type.Value}"
            End If

            x = x.Replace("(Of ", "__ltn__").Replace("of ", "__ltn__").
                 Replace(")", "__gtn__") + vbCrLf + vbCrLf +
                 "<!--This file is auto generated from the .zml file. Don't make any changes here.-->" + vbCrLf + vbCrLf

            model.ReplaceWith(GetXml(x))
        End If
    End Sub

    Private Sub PsrseLoops(xml As XElement)
        Dim foreach = (From elm In xml.Descendants()
                       Where elm.Name = "foreach")?.FirstOrDefault

        If foreach IsNot Nothing Then
            Dim x = $"@foreach (var {foreach.Attribute("var").Value} in {foreach.Attribute("in").Value.Replace("@Model.", "Model.")} )"
            x += vbCrLf + "{" + vbCrLf + "    " + foreach.InnerXML + vbCrLf + "}"
            foreach.ReplaceWith(GetXml(x))

            PsrseLoops(xml)
        End If
    End Sub

    Private Sub PsrseSetters(xml As XElement)

        Dim setter = (From elm In xml.Descendants()
                      Where elm.Name = "set").FirstOrDefault

        If setter IsNot Nothing Then
            Dim key = setter.Attribute("key")
            Dim x = ""
            If key Is Nothing Then
                x = "@{ " + $"{setter.Attribute("object").Value} = '{setter.Attribute("value").Value}';" + " }"
                x = x.Replace("(", "[").Replace(")", "]").Replace("'", """") + vbCrLf
            Else
                x = "@{ " + $"{setter.Attribute("object").Value}['{key.Value}'] = '{setter.Attribute("value").Value}';" + " }"
                x = x.Replace("'", """") + vbCrLf
            End If
            setter.ReplaceWith(x)
            PsrseSetters(xml)
        End If

    End Sub

    Private Sub PsrseGetters(xml As XElement)

        Dim getter = (From elm In xml.Descendants()
                      Where elm.Name = "get").FirstOrDefault

        If getter IsNot Nothing Then
            Dim key = getter.Attribute("key")
            Dim x = ""
            If key Is Nothing Then
                x = $"@{getter.Attribute("object").Value}"
                x = x.Replace("(", "[").Replace(")", "]").Replace("'", """") + vbCrLf
            Else
                x = $"@{getter.Attribute("object").Value}['{key.Value}']"
                x = x.Replace("'", """") + vbCrLf
            End If
            getter.ReplaceWith(x)

            PsrseGetters(xml)
        End If

    End Sub

    Private Sub PsrseConditions(xml As XElement)
        Dim _if = (From elm In xml.Descendants()
                   Where elm.Name = "if")?.FirstOrDefault

        If _if IsNot Nothing Then
            If _if.Nodes.Count > 0 Then
                Dim children = _if.Nodes
                Dim firstChild As XElement = children(0)
                If firstChild.Name = "then" Then
                    Dim _then = "@if (" + convLog(_if.Attribute("condition").Value) + ")"
                    _then += vbCrLf + "{" + vbCrLf + "    " + firstChild.InnerXML + vbCrLf + "}" + vbCrLf

                    Dim _elseifs = PsrseElseIfs(_if)

                    Dim _else = ""
                    Dim lastChild As XElement = children(children.Count - 1)
                    If lastChild.Name = "else" Then
                        _else = "else" + vbCrLf + "{" + vbCrLf + "    " + lastChild.InnerXML + vbCrLf + "}" + vbCrLf
                    End If

                    _if.ReplaceWith(GetXml(_then + vbCrLf + _elseifs + vbCrLf + _else))

                Else
                    Dim x = "@if (" + convLog(_if.Attribute("condition").Value) + ")"
                    x += vbCrLf + "{" + vbCrLf + "    " + firstChild.ToString() + vbCrLf + "}"
                    _if.ReplaceWith(GetXml(x))
                End If
            End If

            PsrseConditions(xml)
        End If

    End Sub

    Private Function convLog(value As String) As String
        Return value.Replace("@Model.", "Model.").
            Replace(" And ", " __amp__ ").Replace(" and ", " __amp__ ").
            Replace(" AndAlso ", " __amp____amp__ ").Replace(" andalso ", " __amp____amp__ ").
            Replace(" Or ", " | ").Replace(" or ", " | ").
            Replace(" OrElse ", " || ").Replace(" orelse ", " || ").
            Replace(" Not ", " !").Replace(" not ", " !").
            Replace(" Xor ", " ^ ").Replace(" xor ", " ^ ").
            Replace(" = ", " == ").Replace(" <> ", " != ").
            Replace(" IsNot ", " != ").Replace(" isnot ", " != ").
            Replace(">", "__gtn__").Replace(">", "__ltn__")
    End Function

    Private Function PsrseElseIfs(xml As XElement) As String
        Dim _elseifs = (From elm In xml.Descendants()
                        Where elm.Name = "elseif")

        Dim sb As New Text.StringBuilder()
        For Each _elseif In _elseifs
            Dim x = "else if (" + convLog(_elseif.Attribute("condition").Value) + ")"
            x += vbCrLf + "{" + vbCrLf + "    " + _elseif.InnerXML + vbCrLf + "}"
            sb.AppendLine(x)
        Next
        sb.AppendLine("")
        Return sb.ToString()
    End Function

End Module
