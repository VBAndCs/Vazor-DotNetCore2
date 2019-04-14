' ZML Parser: Converts ZML tags to C# Razor statements.
' Copyright (c) Mohammad Hamdy Ghanem 2019


Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Xml

Public Module ZML

    Public Const Ampersand = "__amp__"
    Public Const GreaterThan = "__gtn__"
    Public Const LessThan = "__ltn__"
    Public Const TempRootStart = "<zml>"
    Public Const TempRootEnd = "</zml>"
    Public Const SnglQt = "'"
    Public Const Qt = """"

    <Extension>
    Function Replace(s As String, ParamArray repPairs() As (repStr As String, repWithStr As String)) As String
        Dim sb As New Text.StringBuilder(s)
        For Each x In repPairs
            sb.Replace(x.repStr, x.repWithStr)
        Next
        Return sb.ToString()
    End Function

    <Extension>
    Public Function ToXml(x As String) As XElement
        Try
            Return XElement.Parse(x)
        Catch
            Return GetXml(x)
        End Try
    End Function

    <Extension>
    Public Function InnerXML(el As XElement) As String
        Dim X = <zml/>
        X.Add(el.Nodes)
        Return vbCrLf + X.ToString() + vbCrLf
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

    Friend Function ParseZml(zml As String) As String
        Return GetXml(zml).ParseZml()
    End Function

    <Extension>
    Function ParseZml(zml As XElement) As String
        Dim xml = New XElement(zml)
        ParsePage(xml)
        ParseModel(xml)
        ParseViewData(xml)
        ParseTitle(xml)
        PsrseSetters(xml)
        PsrseGetters(xml)
        PsrseConditions(xml)
        PsrseLoops(xml)
        FixTagHelpers(xml)

        Return xml.ToString().
             Replace(
                            (TempRootStart & vbCrLf, ""),
                            (TempRootEnd & vbCrLf, ""),
                            (TempRootStart, ""), (TempRootEnd, ""),
                            (LessThan, "<"), (GreaterThan, ">"),
                            (Ampersand, "&")
                         ).Trim(" ", vbCr, vbLf)
    End Function

    Private Function GetXml(xml As String) As XElement
        Return XElement.Parse(TempRootStart + vbCrLf + xml + vbCrLf + TempRootEnd)
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

                Dim sb As New Text.StringBuilder(vbCrLf + "@{" + vbCrLf)
                For Each o In setter.Attributes
                    sb.AppendLine($"{At(o.Name.ToString())} = {Quote(o.Value)};")
                Next
                sb.AppendLine("}" + vbCrLf)
                x = sb.ToString()

            Else ' Set single value 
                Dim key = setter.Attribute("key")
                Dim value = Quote(If(setter.Attribute("value")?.Value, setter.Value))

                If key Is Nothing Then
                    ' Set single value without key
                    x = "@{ " + $"{At(obj.Value)} = {value};" + " }" + vbCrLf

                Else ' Set single value with key
                    x = "@{ " + $"{At(obj.Value)}[{Quote(key.Value)}] = {value};" + " }" + vbCrLf
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
            Dim obj = At(If(getter.Attribute("object")?.Value, getter.Value))
            Dim x = ""

            If key Is Nothing Then
                x = $"@{obj}" + vbCrLf
            Else
                x = $"@{obj}[{Quote(key.Value)}]" + vbCrLf
            End If
            getter.ReplaceWith(x)

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

                Dim sb As New Text.StringBuilder(vbCrLf + "@{" + vbCrLf)
                For Each key In viewdata.Attributes
                    sb.AppendLine($"ViewData[{At(key.Name.ToString())}] = {Quote(key.Value)};")
                Next
                sb.AppendLine("}" + vbCrLf)
                viewdata.ReplaceWith(GetXml(sb.ToString()))

            ElseIf value IsNot Nothing Then
                ' Write one value to ViewData
                ' <viewdata key="Age" value="15"/>
                ' or <viewdata key="Age">15</viewdata>

                Dim x = $"ViewData[{At(keyAttr.Value)}] = {Quote(value)};"
                viewdata.ReplaceWith(GetXml(x))

            Else ' Read from ViewData
                ' <vewdata key="Age"/>

                Dim x = $"@ViewData[{At(keyAttr.Value)}]"
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
                 (")", GreaterThan)) + vbCrLf + vbCrLf +
                 "<!--This file is auto generated from the .zml file." + vbCrLf +
                 "Make cahnges only to the .zml file, and don't make any changes here," + vbCrLf +
                 "because they will be overwritten when the .zml file changes." + vbCrLf +
                 "If you want to format this file to review some blocks," + vbCrLf +
                 "use the Edit\Advanced\Format Document from main menus. -->" + vbCrLf + vbCrLf

            model.ReplaceWith(GetXml(x))
        End If
    End Sub

    Private Sub PsrseLoops(xml As XElement)
        Dim foreach = (From elm In xml.Descendants()
                       Where elm.Name = "foreach")?.FirstOrDefault

        If foreach IsNot Nothing Then
            Dim _var = At(foreach.Attribute("var").Value)
            Dim _in = foreach.Attribute("in").Value.Replace("@Model.", "Model.")
            Dim x = TempRootStart + $"@foreach (var {_var} in {_in} )" + vbCrLf + "{" + vbCrLf +
                       "    " + TempRootEnd + foreach.InnerXML + TempRootStart + vbCrLf + "}" + vbCrLf + TempRootEnd

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
                    Dim _then = TempRootStart + "@if (" + ConvLog(_if.Attribute("condition").Value) + ")"
                    _then += vbCrLf + "{" + vbCrLf + "    " + TempRootEnd + firstChild.InnerXML + TempRootStart + vbCrLf + "}" + vbCrLf + TempRootEnd

                    Dim _elseifs = PsrseElseIfs(_if)

                    Dim _else = ""
                    Dim lastChild As XElement = children(children.Count - 1)
                    If lastChild.Name = "else" Then
                        _else = TempRootStart + "else" + vbCrLf + "{" + vbCrLf + "    " + TempRootEnd +
                                 lastChild.InnerXML + TempRootStart + vbCrLf + "}" + vbCrLf + TempRootEnd
                    End If

                    _if.ReplaceWith(GetXml(_then + _elseifs + _else))

                Else
                    Dim x = TempRootStart + "@if (" + ConvLog(_if.Attribute("condition").Value) + ")" +
                             vbCrLf + "{" + vbCrLf + "    " + TempRootEnd +
                             firstChild.ToString() + TempRootStart + vbCrLf + "}" + TempRootEnd
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
        For Each _elseif In _elseifs
            x = TempRootStart + "else if (" + ConvLog(_elseif.Attribute("condition").Value) + ")" +
                       vbCrLf + "{" + vbCrLf + "    " + TempRootEnd +
                       _elseif.InnerXML + TempRootStart + vbCrLf + "}" + TempRootEnd
            sb.AppendLine(x)
        Next
        Return sb.ToString()
    End Function

End Module
