' ZML Parser: Converts ZML tags to C# Razor statements.
' Copyright (c) Mohammad Hamdy Ghanem 2019


Imports System.Reflection
Imports System.Runtime.CompilerServices

Public Module ZML

    Public Const Ampersand = "__amp__"
    Public Const GreaterThan = "__gtn__"
    Public Const LessThan = "__ltn__"
    Public Const TempRootStart = "<zml>"
    Public Const TempRootEnd = "</zml>"
    Public Const SnglQt = "'"
    Public Const Qt = """"

    <Extension>
    Function Replace(s As String, ParamArray repPairs() As (repStr As String, repWithStr As String))
        Dim sb As New Text.StringBuilder(s)
        For Each x In repPairs
            sb.Replace(x.repStr, x.repWithStr)
        Next
        Return sb.ToString()
    End Function

    Private Function GetXml(xml As String) As XElement
        Return XElement.Parse(TempRootStart + xml + TempRootEnd)
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
    Function ParseZml(zml As XElement) As String
        Dim xml = New XElement(zml)
        ParsePage(Xml)
        ParseModel(Xml)
        ParseViewData(Xml)
        ParseTitle(Xml)
        PsrseSetters(Xml)
        PsrseGetters(Xml)
        PsrseConditions(Xml)
        PsrseLoops(Xml)
        FixTagHelpers(Xml)

        Return Xml.ToString(SaveOptions.DisableFormatting).
            Replace(
                            (TempRootStart, ""), (TempRootEnd, ""),
                            (LessThan, "<"), (GreaterThan, ">"),
                            (Ampersand, "&")
                         )
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
                title = "@{ " & $"ViewData[{Qt}Title{Qt}] = {Qt & value & Qt};" & " }"
            End If

            viewTitle.ReplaceWith(GetXml(title))

            ParseTitle(xml)
        End If

    End Sub

    Private Sub PsrseSetters(xml As XElement)

        Dim setter = (From elm In xml.Descendants()
                      Where elm.Name = "set").FirstOrDefault

        If setter IsNot Nothing Then
            Dim x = ""

            Dim obj = setter.Attribute("object")
            If obj Is Nothing Then
                ' Set multiple values
                ' <set x="3" y="arr[3]" z='dict["key"]' myChar="'a'" name="'student'"  obj = "Student" />

                Dim sb As New Text.StringBuilder(vbCrLf + "@{" + vbCrLf)
                For Each o In setter.Attributes
                    sb.AppendLine($"{o.Name} = {o.Value};")
                Next
                sb.AppendLine("}" + vbCrLf)
                x = sb.ToString()

            Else ' Set single value 
                Dim key = setter.Attribute("key")
                If key Is Nothing Then
                    ' Set single value without key
                    ' <set obj="arr">new string(){}</set>

                    x = "@{ " + $"{obj.Value} = {setter.Attribute("value").Value};" + " }"
                    x = x.Replace(("(", "["), (")", "]")) + vbCrLf
                Else ' Set single value with key
                    ' <set obj="dect" key="Name">"Ali"</set>

                    x = "@{ " + $"{obj.Value}[{Qt}{key.Value}{Qt}] = {setter.Attribute("value").Value};" + " }" + vbCrLf
                End If
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
            Dim obj = getter.Attribute("object").Value

            If key Is Nothing Then
                x = $"@{obj}" + vbCrLf
            Else
                x = $"@{obj}[{key.Value}]" + vbCrLf
            End If
            getter.ReplaceWith(x)

            PsrseGetters(xml)
        End If

    End Sub

    Private Sub ParseViewData(xml As XElement)
        Dim viewdata = (From elm In xml.Descendants()
                        Where elm.Name = "viewdata")?.FirstOrDefault

        If viewdata IsNot Nothing Then
            Dim strKey = viewdata.Attribute("key")
            Dim value = viewdata.Attribute("value")

            If strKey Is Nothing Then
                ' Write miltiple values to ViewData
                ' <viewdata Name="'Ali'" Age= "15"/>

                Dim sb As New Text.StringBuilder(vbCrLf + "@{" + vbCrLf)
                For Each key In viewdata.Attributes
                    sb.AppendLine($"ViewData[{Qt }{key.Name}{Qt}] = {key.Value};")
                Next
                sb.AppendLine("}" + vbCrLf)
                viewdata.ReplaceWith(GetXml(sb.ToString()))

            ElseIf value IsNot Nothing Then
                ' Write one value to ViewData
                ' <viewdata key="Age" value="15"/>
                ' or <viewdata key="Age">15</viewdata>

                Dim x = $"ViewData[{strKey.Value}] = {value.Value};"
                viewdata.ReplaceWith(GetXml(x))

            Else ' Read from ViewData
                ' <vewdata key="Age"/>

                Dim x = $"@ViewData[{strKey.Value }]"
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
            If type Is Nothing Then
                x += $"{model.Value}"
            Else
                x += $"{type.Value}"
            End If

            x = x.Replace("(Of ", LessThan).Replace("of ", LessThan).
                 Replace(")", GreaterThan) + vbCrLf + vbCrLf +
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
        For Each _elseif In _elseifs
            Dim x = "else if (" + convLog(_elseif.Attribute("condition").Value) + ")"
            x += vbCrLf + "{" + vbCrLf + "    " + _elseif.InnerXML + vbCrLf + "}"
            sb.AppendLine(x)
        Next
        sb.AppendLine("")
        Return sb.ToString()
    End Function

End Module
