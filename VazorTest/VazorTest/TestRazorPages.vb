Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Vazor

<TestClass>
Public Class TestRazorPages

    <TestMethod>
    Sub TestSections()
        Dim x = <zml xmlns:z="zml">
                    <z:section name="Scripts">
                        <partial name="_ValidationScriptsPartial"/>
                    </z:section>
                </zml>

        Dim y = x.ParseZml.ToString()
        Dim z =
"@section Scripts
{
  <partial name='_ValidationScriptsPartial' />
}".Replace(SnglQt, Qt)

        Assert.AreEqual(y, z)
    End Sub


    <TestMethod>
    Sub TestTagHelpers()
        Dim x = <zml xmlns:z="zml">
                    <p asp-for="students"
                        asp-items="Model.students"
                    />
                </zml>

        Dim p As XElement = x.ParseZml().ToXml().FirstNode
        Assert.AreEqual(p.Attribute("asp-for").Value, "@Model.students")
        Assert.AreEqual(p.Attribute("asp-items").Value, "@Model.students")

        x = <zml xmlns:z="zml">
                <p
                    asp-for="@students"
                    asp-items="@Model.students"
                />
            </zml>

        p = x.ParseZml().ToXml().FirstNode
        Assert.AreEqual(p.Attribute("asp-for").Value, "@students")
        Assert.AreEqual(p.Attribute("asp-items").Value, "@Model.students")

    End Sub

    <TestMethod>
    Sub TestTitle()
        ' Set string title
        Dim x = <zml xmlns:z="zml">
                    <z:title>test</z:title>
                </zml>

        Dim y = x.ParseZml()
        Dim z = "@{ " & $"ViewData[{Qt}Title{Qt}] = {Qt}test{Qt};" & " }"
        Assert.AreEqual(y, z)

        ' Set string title
        x = <zml xmlns:z="zml">
                <z:title value="test"/>
            </zml>
        y = x.ParseZml()
        Assert.AreEqual(y, z)

        ' Read title
        x = <zml xmlns:z="zml">
                <z:title/>
            </zml>
        y = x.ParseZml()
        z = $"@ViewData[{Qt }Title{Qt }]"
        Assert.AreEqual(y, z)

        ' Set title from variable
        x = <zml xmlns:z="zml">
                <z:title>@Title</z:title>
            </zml>
        y = x.ParseZml()
        z = "@{ " & $"ViewData[{Qt}Title{Qt}] = Title;" & " }"
        Assert.AreEqual(y, z)

        ' Set title from model property
        x = <zml xmlns:z="zml">
                <z:title value="@Model.Title"/>
            </zml>
        y = x.ParseZml()
        z = "@{ " & $"ViewData[{Qt}Title{Qt}] = Model.Title;" & " }"
        Assert.AreEqual(y, z)
    End Sub

    <TestMethod>
    Sub TestComments()
        Dim x =
                <zml xmlns:z="zml">
                    <p>test</p>
                    <z:comment>comment</z:comment>
                    <z:comment>
                        <p>test comment</p>
                    </z:comment>
                </zml>

        Dim y = x.ParseZml.ToString()
        Dim z =
"<p>test</p>
@*
comment
*@
@*
  <p>test comment</p>
*@"

        Assert.AreEqual(y, z)
    End Sub

    <TestMethod>
    Sub TestInjects()
        Dim x = <zml xmlns:z="zml">
                    <z:inject A.type="T(Of Integer)"
                        B.type="string"
                    />
                </zml>

        Dim y = x.ParseZml.ToString()
        Dim z =
"@inject T<int> A
@inject string B"

        Assert.AreEqual(y, z)

    End Sub


    <TestMethod>
    Sub TestPage()
        Dim x = <zml xmlns:z="zml">
                    <z:page/>
                </zml>
        Dim y = x.ParseZml().ToXml()
        Dim z = y.Value.Trim()
        Assert.AreEqual(z, "@page")

        x = <zml xmlns:z="zml"><z:page>Pages/Home</z:page></zml>
        y = x.ParseZml().ToXml()
        z = y.Value.Trim()
        Assert.AreEqual(z, $"@page {Qt}Pages/Home{Qt}")

        x = <zml xmlns:z="zml"><z:page route="Pages/Home"/></zml>
        y = x.ParseZml().ToXml()
        z = y.Value.Trim()
        Assert.AreEqual(z, $"@page {Qt}Pages/Home{Qt}")

    End Sub

    <TestMethod>
    Sub TestModel()
        Dim x = <zml xmlns:z="zml">
                    <z:model>IndexModel</z:model>
                </zml>

        Dim y = x.ParseZml()
        Assert.IsTrue(y.Contains("@model IndexModel"))

        x = <zml xmlns:z="zml"><z:model type="IndexModel"/></zml>
        y = x.ParseZml()
        Assert.IsTrue(y.Contains("@model IndexModel"))

    End Sub



    <TestMethod>
    Sub TestLayout()
        Dim x = <zml xmlns:z="zml">
                    <z:layout>_Layout</z:layout>
                </zml>

        Dim y = x.ParseZml.ToString()
        Dim z =
"@{
    Layout = '_Layout';
}".Replace(SnglQt, Qt)

        Assert.AreEqual(y, z)

        x = <zml xmlns:z="zml">
                <z:layout page="_Layout"/>
            </zml>

        y = x.ParseZml.ToString()

        Assert.AreEqual(y, z)
    End Sub

    <TestMethod>
    Sub TestTexts()
        Dim x = <zml xmlns:z="zml">
                    <z:text>__amp__;nbsp;</z:text>
                </zml>

        Dim y = x.ParseZml().ToString()
        Dim z = "@: &nbsp;"
        Assert.AreEqual(y, z)

    End Sub

    <TestMethod>
    Sub TestClosingTags()
        Dim x = <zml xmlns:z="zml">
                    <span/>
                    <label id="1"/>
                </zml>

        Dim y = x.ParseZml().ToString()
        Dim z =
"<span></span>
<label id='1'></label>".Replace(SnglQt, Qt)
        Assert.AreEqual(y, z)

    End Sub
End Class
