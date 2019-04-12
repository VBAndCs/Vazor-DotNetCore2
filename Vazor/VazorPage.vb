Public Class VazorPage
    Inherits VazorView

    Public Sub New(name As String, path As String, title As String, html As String, Optional encoding As Text.Encoding = Nothing)
        MyBase.New(name, path, title, If(encoding, Text.Encoding.UTF8))
        Me.Content = MyBase.Encoding.GetBytes(html)
    End Sub

    ' This function is called in the "Index" action method in the HomeController:
    ' View(IndexView.CreateNew(Students, ViewData))

    Public Shared Function CreateNew(name As String, path As String, title As String,
                                     html As String, Optional encoding As Text.Encoding = Nothing) As String

        Return VazorViewMapper.Add(New VazorPage(name, path, title, html, encoding))
    End Function

    Public Shared Function CreateNew(zmlFile As String, Optional encoding As Text.Encoding = Nothing) As String
        Dim zml = ""
        If encoding Is Nothing Then
            zml = IO.File.OpenText(zmlFile).ReadToEnd()
        Else
            zml = New IO.StreamReader(zmlFile, encoding).ReadToEnd()
        End If

        Dim name = IO.Path.GetFileNameWithoutExtension(zmlFile)
        Dim view = New VazorPage(Name, zmlFile, "", ParseZml(zml), encoding)
        Return VazorViewMapper.Add(view)
    End Function

    Public Overrides ReadOnly Property Content() As Byte()

End Class
