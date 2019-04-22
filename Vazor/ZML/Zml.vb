
Public Class Zml
    ' Note: All the wotk is done in the partial file ZmlParsers.vb

    Dim CsCode As New List(Of String)
    Dim BlockStart, BlockEnd As XElement
    Dim Xml As XElement
    Const TempBodyStart = "<zmlbody>"
    Const TempBodyEnd = "</zmlbody>"
    Const ChngQt = "__chngqt__"

    Private Function AddToCsList(item As String, Optional parent As XElement = Nothing) As XElement
        If parent IsNot Nothing Then
            If IsInsideCsBlock(parent.Parent) Then item = item.TrimStart("@"c, "{"c).TrimEnd("}"c)
        End If

        CsCode.Add(item)
        Dim zmlKey = "zmlitem" & CsCode.Count - 1
        Return <<%= zmlKey %>/>
    End Function


    Friend Shared Function FixAttr(x As String) As String

        Dim lines = x.Replace(("&", Ampersand), ("@", AtSymbole)).
        Split({CChar(vbCr), CChar(vbLf)}, StringSplitOptions.RemoveEmptyEntries)
        Dim sb As New Text.StringBuilder

        For Each line In lines
            Dim absLine = line.TrimStart()
            Dim spaces = New String(" ", line.Length - absLine.Length)
            Do
                Dim L = absLine.Length
                absLine = absLine.Replace(("< ", "<"), (" >", ">"))
                If L = absLine.Length Then Exit Do
            Loop
            sb.AppendLine(spaces + absLine)
        Next

        x = sb.ToString()

        Dim tags = {lambdaTag, usingTag, importsTag, namespaceTag, helpersTag}

        For Each tag In tags
            Dim lambdaCase = (tag = lambdaTag)
            tag = tag.Replace(zns, "z:")
            Dim pos = 0
            Dim offset = 0
            Dim endPos = 0
            Do
                pos = x.IndexOf("<" & tag + " ", endPos)
                If pos = -1 Then Exit Do
                offset = pos + tag.Length + 2
                endPos = x.IndexOf("/>", offset)
                If endPos > -1 Then
                    Dim s = x.Substring(offset, endPos - offset)
                    Dim t = s
                    If lambdaCase Then t = t.Replace("return=", "")

                    If Not t.Contains("=") Then
                        Dim attrs = s.Split(" "c, CChar(vbCr), CChar(vbLf))
                        sb = New Text.StringBuilder
                        For Each attr In attrs
                            If attr <> "" And Not attr.Contains("=") Then sb.AppendLine(attr + "=" + Qt + Qt)
                        Next
                        x = x.Substring(0, offset) + sb.ToString().TrimEnd(vbCr, vbLf) + x.Substring(endPos)
                        endPos += 2
                    End If
                End If
            Loop
        Next


        ' FixConditions

        Return x
    End Function

    ' Qute string values, except objects (starting with @) and chars (quted by ' ')
    Private Function Quote(value As String) As String
        If value Is Nothing Then Return Nothing

        If value.StartsWith(AtSymbole) Then
            Return value.Substring(AtSymbole.Length) ' value is object
        ElseIf value.StartsWith("@") Then ' This is for test app
            Return value.Substring(1) ' value is object 
        ElseIf value.StartsWith(SnglQt) AndAlso value.EndsWith(SnglQt) Then
                Return value ' value is char
        ElseIf value.StartsWith("#") AndAlso value.EndsWith("#") Then
            Return $"DateTime.Parse({Qt}{value.Trim("#")}{Qt})"
        ElseIf value = trueKeyword OrElse value = falseKeyword Then
            Return value ' value is boolean  
        ElseIf Double.TryParse(value, New Double()) Then
            Return value ' value is numeric       
        Else
            Return Qt + value.Trim(Qt) + Qt ' value is string
        End If
    End Function

    ' vars are always objects. Erase @ and qoutes
    Private Function At(value As String) As String
        If value Is Nothing Then Return Nothing

        If value.StartsWith(AtSymbole) Then
            Return value.Substring(AtSymbole.Length)
        ElseIf value.StartsWith("@") Then ' This is for test app
            Return value.Substring(1) ' value is object 
        Else
            Return value.Trim(Qt).Trim(SnglQt)
        End If
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
                    AddToCsList(cs, html),
                     BlockStart,
                        If(UseInner, html.InnerXml, html),
                     BlockEnd
                  )
        Return x
    End Function


    Private Function IsInsideCsBlock(item As XElement) As Boolean
        If item Is Nothing Then Return False
        Dim pn As XElement = item.Parent
        If pn Is Nothing Then Return False

        Dim parentName = pn.Name.ToString
        If parentName.StartsWith(zns) Then Return True

        If parentName = "zmlbody" OrElse (parentName = "zml" AndAlso pn.Nodes.Count = 1) Then pn = pn.Parent
        If pn Is Nothing Then Return False
        If parentName.StartsWith(zns) Then Return True

        Dim blkSt = BlockStart.Name.ToString()

        For Each x As XElement In pn.Nodes
            If x.Name.ToString() = blkSt Then Return True
        Next

        Return False
    End Function

End Class
