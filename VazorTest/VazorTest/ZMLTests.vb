Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Vazor

Namespace VazorTest
    <TestClass>
    Public Class UnitTest1

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

            Dim p = x.ParseZml().ToXml()
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

        <TestMethod>
        Sub TestSetters()
            Dim x = <zml>
                        <set
                            d="4/1/2019"
                            d2="#4/1/2019#"
                            n="3"
                            s='"3"'
                            y="@arr[3]"
                            z='@dict["key"]'
                            myChar="'a'"
                            name="student"
                            obj="@Student"/>
                    </zml>

            Dim y = x.ParseZml().ToXml()
            Dim s = y.Value.Trim()
            Assert.IsTrue(s.StartsWith("@{") And s.EndsWith("}"))
            Dim lines = s.Trim("@", "{", "}", vbCr, vbLf).Replace(vbCrLf, vbLf).Split(vbLf, StringSplitOptions.RemoveEmptyEntries)
            Assert.AreEqual(lines(0), $"d = {Qt}4/1/2019{Qt};")
            Assert.AreEqual(lines(1), $"d2 = DateTime.Parse({Qt}4/1/2019{Qt});")
            Assert.AreEqual(lines(2), "n = 3;")
            Assert.AreEqual(lines(3), $"s = {Qt}3{Qt};")
            Assert.AreEqual(lines(4), "y = arr[3];")
            Assert.AreEqual(lines(5), $"z = dict[{Qt}key{Qt}];")
            Assert.AreEqual(lines(6), "myChar = 'a';")
            Assert.AreEqual(lines(7), $"name = {Qt}student{Qt};")
            Assert.AreEqual(lines(8), "obj = Student;")

            x = <zml><set object="arr" value="new String(){}"/></zml>
            Dim z = x.ParseZml()
            Assert.AreEqual(z, $"@{{ arr = {Qt}new String(){{}}{Qt}; }}")

            x = <zml><set object="arr" value="@new String[]{}"/></zml>
            z = x.ParseZml()
            Assert.AreEqual(z, $"@{{ arr = new String[]{{}}; }}")

            x = <zml><set object="dect" key="Name">Adam</set></zml>
            z = x.ParseZml()
            Assert.AreEqual(z, $"@{{ dect[{Qt}Name{Qt}] = {Qt}Adam{Qt}; }}")

            x = <zml><set object="dect" key="Name" value="Adam"/></zml>
            z = x.ParseZml()
            Assert.AreEqual(z, $"@{{ dect[{Qt}Name{Qt}] = {Qt}Adam{Qt}; }}")

            x = <zml><set object="dect" key="@Name">@Adam</set></zml>
            z = x.ParseZml()
            Assert.AreEqual(z, $"@{{ dect[Name] = Adam; }}")

        End Sub

    End Class
End Namespace

