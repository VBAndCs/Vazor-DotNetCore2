Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Vazor

Namespace VazorTest
    <TestClass>
    Public Class UnitTest1

        Private Function GetXml(xml As String) As XElement
            Return XElement.Parse(TempRootStart + xml + TempRootEnd)
        End Function


        <TestMethod>
        Sub TestReplace()
            Dim x = "Test multiple replacements in string"
            Dim y = x.Replace(("replacements", ""), ("string", "strings"))
            Dim z = "Test multiple  in strings"
            Assert.AreEqual(y, z)
        End Sub

        <TestMethod>
        Sub TestTagHelpers()
            Dim x = <zml>
                        <p asp-a="students"
                            asp-b="@students"
                            asp-c="Model.students"
                            asp-d="@Model.students"
                        />
                    </zml>

            Dim y = GetXml(x.ParseZml())
            Dim p As XElement = y.FirstNode
            Assert.AreEqual(p.Attribute("asp-a").Value, "@Model.students")
            Assert.AreEqual(p.Attribute("asp-b").Value, "@students")
            Assert.AreEqual(p.Attribute("asp-c").Value, "@Model.students")
            Assert.AreEqual(p.Attribute("asp-d").Value, "@Model.students")

        End Sub

        <TestMethod>
        Sub TestViewTitle()
            ' Set string title
            Dim x = <zml><viewtitle>test</viewtitle></zml>
            Dim y = x.ParseZml()
            Dim z = "@{ " & $"ViewData[{Qt}Title{Qt}] = {Qt}test{Qt};" & " }"
            Assert.IsTrue(y.Contains(z))

            ' Set string title
            x = <zml><viewtitle value="test"/></zml>
            y = x.ParseZml()
            Assert.IsTrue(y.Contains(z))

            ' Get string title
            x = <zml><viewtitle/></zml>
            y = x.ParseZml()
            z = $"@ViewData[{Qt }Title{Qt }]"
            Assert.IsTrue(y.Contains(z))

            ' Set title from variable
            x = <zml><viewtitle>@Title</viewtitle></zml>
            y = x.ParseZml()
            z = "@{ " & $"ViewData[{Qt}Title{Qt}] = Title;" & " }"
            Assert.IsTrue(y.Contains(z))

            ' Set title from model property
            x = <zml><viewtitle value="@Model.Title"/></zml>
            y = x.ParseZml()
            z = "@{ " & $"ViewData[{Qt}Title{Qt}] = Model.Title;" & " }"
            Assert.IsTrue(y.Contains(z))
        End Sub

    End Class
End Namespace

