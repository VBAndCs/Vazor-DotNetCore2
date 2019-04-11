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

    <Extension>
    Function ParseZml(xml As XElement) As String
        ParseModel(xml)
        PsrseSetters(xml)
        PsrseConditions(xml)
        PsrseLoops(xml)
        Return xml.ToString().
            Replace("<zml>", "").Replace("</zml>", "").
            Replace("__OfStart__", "<").Replace("__OfEnd__", ">")
    End Function

    Private Sub ParseModel(xml As XElement)
        Dim model = (From elm In xml.Descendants()
                     Where elm.Name = "model")?.FirstOrDefault

        If model IsNot Nothing Then
            Dim x = "@model " + $"{model.Attribute("type").Value}"
            x = x.Replace("(Of ", "__OfStart__").Replace("of ", "__OfStart__").Replace(")", "__OfEnd__") + vbCrLf
            model.ReplaceWith(GetXml(x))
        End If
    End Sub

    Private Sub PsrseLoops(xml As XElement)
        Dim foreach = (From elm In xml.Descendants()
                       Where elm.Name = "foreach")?.FirstOrDefault

        If foreach IsNot Nothing Then
            Dim x = $"@foreach (var {foreach.Attribute("var").Value} in {foreach.Attribute("in").Value} )"
            x += vbCrLf + "{" + vbCrLf + "    " + foreach.InnerXML + vbCrLf + "}"
            foreach.ReplaceWith(GetXml(x))

            PsrseConditions(xml)
        End If
    End Sub

    Private Sub PsrseSetters(xml As XElement)

        Dim setter = (From elm In xml.Descendants()
                      Where elm.Name = "set").FirstOrDefault

        If setter IsNot Nothing Then
            Dim x = "@{ " + $"{setter.Attribute("object").Value} = '{setter.Attribute("value").Value}';" + " }"
            x = x.Replace("(", "[").Replace(")", "]").Replace("'", ChrW(34)) + vbCrLf
            setter.ReplaceWith(x)
            PsrseSetters(xml)
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
                    Dim _then = "@if (" + _if.Attribute("condition").Value + ")"
                    _then += vbCrLf + "{" + vbCrLf + "    " + firstChild.InnerXML + vbCrLf + "}" + vbCrLf

                    Dim _elseifs = PsrseElseIfs(_if)

                    Dim _else = ""
                    Dim lastChild As XElement = children(children.Count - 1)
                    If lastChild.Name = "else" Then
                        _else = "else" + vbCrLf + "{" + vbCrLf + "    " + lastChild.InnerXML + vbCrLf + "}" + vbCrLf
                    End If

                    _if.ReplaceWith(GetXml(_then + vbCrLf + _elseifs + vbCrLf + _else))

                Else
                    Dim x = "@if (" + _if.Attribute("condition").Value + ")"
                    x += vbCrLf + "{" + vbCrLf + "    " + firstChild.ToString() + vbCrLf + "}"
                    _if.ReplaceWith(GetXml(x))
                End If
            End If

            PsrseConditions(xml)
        End If

    End Sub

    Private Function PsrseElseIfs(xml As XElement) As String
        Dim _elseifs = (From elm In xml.Descendants()
                        Where elm.Name = "elseif")

        Dim sb As New Text.StringBuilder()
        For Each _elseif In _elseifs
            Dim x = "else if (" + _elseif.Attribute("condition").Value + ")"
            x += vbCrLf + "{" + vbCrLf + "    " + _elseif.InnerXML + vbCrLf + "}"
            sb.AppendLine(x)
        Next

        Return sb.ToString()
    End Function

End Module
