﻿Imports Microsoft.AspNetCore.Mvc.ViewFeatures
Imports Vazor

' To add anew vzor view, right-click the folder in solution explorer
' click Add/New item, and chosse the "VazorView" item from the window
' This will add both the vazor.vb and vbxml.vb files to the folder.

Public Class IndexView
    Inherits VazorView

    Public ReadOnly Property Students As List(Of Student)

    Public ReadOnly Property ViewData() As ViewDataDictionary

    ' Supply your actual view name, path, and title to the MyBas.New
    ' By defualt, UTF encoding is used to render the view. 
    ' You can send another encoding to the forth param of the MyBase.New.

    Public Sub New(students As List(Of Student), viewData As ViewDataDictionary)
        MyBase.New("Index", "Views\Home", "Hello")
        Me.Students = students
        Me.ViewData = viewData
        viewData("Title") = Title
    End Sub

    ' This function is called in the "Index" action method in the HomeController:
    ' View(IndexView.CreateNew(Students, ViewData))

    Public Shared Function CreateNew(Students As List(Of Student), viewData As ViewDataDictionary) As String
        Return VazorViewMapper.Add(New IndexView(Students, viewData))
    End Function

End Class
