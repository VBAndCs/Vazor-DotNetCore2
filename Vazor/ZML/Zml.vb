Public Class Zml
    ' Note: All the wotk is done in the partial file ZmlParsers.vb

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

    Private Function AddToCsList(item As String) As XElement
        CsCode.Add(item)
        Dim zmlKey = "zmlitem" & CsCode.Count - 1
        Return <<%= zmlKey %>/>
    End Function


    Friend Shared Function FixAttr(x As String) As String
        Do
            Dim L = x.Length
            x = x.Replace(("  ", " "), ("< ", "<"), (" >", ">"))
            If L = x.Length Then Exit Do
        Loop

        Dim tags = {usingTag, importsTag, namespaceTag, helpersTag}
        For Each tag In tags
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
                    If Not s.Contains("=") Then
                        Dim attrs = s.Split(" "c, CChar(vbCr), CChar(vbLf))
                        Dim sb As New Text.StringBuilder
                        For Each attr In attrs
                            If attr <> "" Then sb.AppendLine(attr + "=" + Qt + Qt)
                        Next
                        x = x.Substring(0, offset) + sb.ToString().TrimEnd(vbCr, vbLf) + x.Substring(endPos)
                        endPos += 2
                    End If
                End If
            Loop
        Next
        Return x
    End Function

    Public Sub New(Optional addComment As Boolean = True)
        Me.AddComment = addComment
    End Sub

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
        If value Is Nothing Then Return Nothing

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


    Private Function IsInsideCsBlock(item As XElement) As Boolean
        Dim pn As XElement = item.Parent
        If pn Is Nothing Then Return False

        Dim parentName = pn.Name.ToString
        If parentName = "zmlbody" OrElse parentName = "zml" Then pn = pn.Parent
        If pn Is Nothing Then Return False

        Dim blkSt = BlockStart.Name.ToString()

        For Each x As XElement In pn.Nodes
            If x.Name.ToString() = blkSt Then Return True
        Next

        Return False
    End Function

End Class
