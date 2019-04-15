' ZML Parser: Converts ZML tags to C# Razor statements.
' Copyright (c) Mohammad Hamdy Ghanem 2019


Imports System.Runtime.CompilerServices

Public Module ZMLExtentions

    Public Const Ampersand = "__amp__"
    Public Const GreaterThan = "__gtn__"
    Public Const LessThan = "__ltn__"
    Public Const TempRootStart = "<zml>"
    Public Const TempRootEnd = "</zml>"
    Public Const SnglQt = "'"
    Public Const Qt = """"
    Public Const Ln = vbCrLf

    <Extension>
    Function Replace(s As String, ParamArray repPairs() As (repStr As String, repWithStr As String)) As String
        For Each x In repPairs
            s = s.Replace(x.repStr, x.repWithStr)
        Next
        Return s
    End Function

    <Extension>
    Public Function ToXml(x As String) As XElement
        Try
            Return XElement.Parse(x)
        Catch
            Return XElement.Parse(TempRootStart + vbCrLf + x + vbCrLf + TempRootEnd)
        End Try
    End Function

    <Extension>
    Public Function GetInnerXML(el As XElement) As String
        Return InnerXML(el).
                   Replace((TempRootStart, ""), (TempRootEnd, "")).
                   Trim(" ", vbCr, vbLf)
    End Function

    <Extension>
    Friend Function InnerXML(el As XElement) As String
        Dim x = <zml/>
        x.Add(el.Nodes)
        Return Ln + x.ToString() + Ln
    End Function

    <Extension>
    Function ParseZml(zml As XElement, Optional addComment As Boolean = False) As String
        Return New Zml(addComment).ParseZml(zml)
    End Function

    <Extension>
    Friend Function ParseZml(zml As String) As String
        Return ToXml(zml).ParseZml(True)
    End Function
End Module
