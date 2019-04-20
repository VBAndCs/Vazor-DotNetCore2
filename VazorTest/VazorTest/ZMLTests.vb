Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Vazor

Namespace VazorTest
    <TestClass>
    Public Class ZmlUnitTest

        <TestMethod>
        Sub TestReplace()
            Dim x = "Test multiple replacements in string"
            Dim y = x.Replace(("replacements", ""), ("string", "strings"))
            Dim z = "Test multiple  in strings"
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
        Sub TestDeclarations()
            Dim x = <zml xmlns:z="zml">
                        <z:declare
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
            Assert.AreEqual(lines(0), $"var d = {Qt}4/1/2019{Qt};")
            Assert.AreEqual(lines(1), $"var d2 = DateTime.Parse({Qt}4/1/2019{Qt});")
            Assert.AreEqual(lines(2), "var n = 3;")
            Assert.AreEqual(lines(3), $"var s = {Qt}3{Qt};")
            Assert.AreEqual(lines(4), "var y = arr[3];")
            Assert.AreEqual(lines(5), $"var z = dict[{Qt}key{Qt}];")
            Assert.AreEqual(lines(6), "var myChar = 'a';")
            Assert.AreEqual(lines(7), $"var name = {Qt}student{Qt};")
            Assert.AreEqual(lines(8), "var obj = Student;")

            x = <zml xmlns:z="zml"><z:declare var="arr" value="new String(){}"/></zml>
            Dim z = x.ParseZml()
            Assert.AreEqual(z, $"@{{ var arr = {Qt}new String(){{}}{Qt}; }}")

            x = <zml xmlns:z="zml"><z:declare var="arr" value="@new String[]{}"/></zml>
            z = x.ParseZml()
            Assert.AreEqual(z, $"@{{ var arr = new String[]{{}}; }}")

            x = <zml xmlns:z="zml"><z:declare var="Name" key="Adam">dict</z:declare></zml>
            z = x.ParseZml()
            Assert.AreEqual(z, $"@{{ var Name = dict[{Qt}Adam{Qt}]; }}")

            x = <zml xmlns:z="zml">
                    <z:declare var="Name" value="dict" key="Adam"/>
                </zml>
            z = x.ParseZml()
            Assert.AreEqual(z, $"@{{ var Name = dict[{Qt}Adam{Qt}]; }}")

            x = <zml xmlns:z="zml"><z:declare var="Name" key="@Adam">@dict</z:declare></zml>
            z = x.ParseZml()
            Assert.AreEqual(z, $"@{{ var Name = dict[Adam]; }}")

            x = <zml xmlns:z="zml">
                    <z:declare var="Sum">
                        <z:lambda a.type="int" b.type="integer" return="a + b"/>
                    </z:declare>
                </zml>
            z = x.ParseZml()

            Assert.AreEqual(z, $"@{{ var Sum = (int a, int b) => a + b; }}")


            ' Test Types
            ' ------------------------
            x = <zml xmlns:z="zml"><z:declare var="arr" type="int" value="new String(){}"/></zml>
            z = x.ParseZml()
            Assert.AreEqual(z, $"@{{ int arr = {Qt}new String(){{}}{Qt}; }}")

            x = <zml xmlns:z="zml"><z:declare var="arr" type="Int32" value="@new String[]{}"/></zml>
            z = x.ParseZml()
            Assert.AreEqual(z, $"@{{ Int32 arr = new String[]{{}}; }}")

            x = <zml xmlns:z="zml"><z:declare var="Name" type="Integer" key="Adam">dict</z:declare></zml>
            z = x.ParseZml()
            Assert.AreEqual(z, $"@{{ int Name = dict[{Qt}Adam{Qt}]; }}")

            x = <zml xmlns:z="zml"><z:declare var="Name" type="Long" value="dict" key="Adam"/></zml>
            z = x.ParseZml()
            Assert.AreEqual(z, $"@{{ long Name = dict[{Qt}Adam{Qt}]; }}")

            x = <zml xmlns:z="zml"><z:declare var="Name" type="List(Of Single, UInteger)" key="@Adam">@dict</z:declare></zml>
            z = x.ParseZml()
            Assert.AreEqual(z, $"@{{ List<float, uint> Name = dict[Adam]; }}")

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
        Sub TestSetters()
            Dim x = <zml xmlns:z="zml">
                                <z:set
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

            x = <zml xmlns:z="zml"><z:set object="arr" value="new String(){}"/></zml>
            Dim z = x.ParseZml()
            Assert.AreEqual(z, $"@{{ arr = {Qt}new String(){{}}{Qt}; }}")

            x = <zml xmlns:z="zml"><z:set object="arr" value="@new String[]{}"/></zml>
            z = x.ParseZml()
            Assert.AreEqual(z, $"@{{ arr = new String[]{{}}; }}")

            x = <zml xmlns:z="zml"><z:set object="dict" key="Name">Adam</z:set></zml>
            z = x.ParseZml()
            Assert.AreEqual(z, $"@{{ dict[{Qt}Name{Qt}] = {Qt}Adam{Qt}; }}")

            x = <zml xmlns:z="zml"><z:set object="dict" key="Name" value="Adam"/></zml>
            z = x.ParseZml()
            Assert.AreEqual(z, $"@{{ dict[{Qt}Name{Qt}] = {Qt}Adam{Qt}; }}")

            x = <zml xmlns:z="zml"><z:set object="dict" key="@Name">@Adam</z:set></zml>
            z = x.ParseZml()
            Assert.AreEqual(z, $"@{{ dict[Name] = Adam; }}")

            x = <zml xmlns:z="zml">
                                <z:set object="Sum">
                                    <z:lambda a.type="int" b.type="integer" return="a + b"/>
                                </z:set>
                            </zml>
            z = x.ParseZml()

            Assert.AreEqual(z, $"@{{ Sum = (int a, int b) => a + b; }}")

        End Sub

        <TestMethod>
        Sub TestGetters()
            Dim x = <zml xmlns:z="zml"><z:get>X</z:get></zml>
            Dim y = x.ParseZml().ToXml()
            Dim z = y.Value.Trim()
            Assert.AreEqual(z, "@X")

            x = <zml xmlns:z="zml">
                                    <z:get object="X"/>
                                </zml>

            y = x.ParseZml().ToXml()
            z = y.Value.Trim()
            Assert.AreEqual(z, "@X")

            x = <zml xmlns:z="zml"><z:get object="X" key="@i"/></zml>
            y = x.ParseZml().ToXml()
            z = y.Value.Trim()
            Assert.AreEqual(z, "@X[i]")

            x = <zml xmlns:z="zml"><z:get object="X" key="name"/></zml>
            y = x.ParseZml().ToXml()
            z = y.Value.Trim()
            Assert.AreEqual(z, $"@X[{Qt}name{Qt}]")

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
        Sub TestForLoop()
            Dim lp =
                <zml xmlns:z="zml">
                                                <z:for i="0" to="10">
                                                    <p>@i</p>
                                                </z:for>
                                            </zml>

            Dim y = lp.ParseZml()
            Dim z =
"@for (var i = 0; i < 10 + 1; i++)
{
  <p>@i</p>
}"

            lp = <zml xmlns:z="zml">
                                                    <z:for i="0" to="@Model.Count - 1">
                                                        <p>@i</p>
                                                    </z:for>
                                                </zml>

            y = lp.ParseZml()
            z =
"@for (var i = 0; i < Model.Count; i++)
{
  <p>@i</p>
}"

            Assert.AreEqual(y, z)

            lp = <zml xmlns:z="zml">
                                                        <z:for i="0" while=<%= "i < Model.Count" %> let="i++">
                                                            <p>@i</p>
                                                        </z:for>
                                                    </zml>

            y = lp.ParseZml()
            z =
"@for (var i = 0; i < Model.Count; i++)
{
  <p>@i</p>
}"

            Assert.AreEqual(y, z)

            lp = <zml xmlns:z="zml">
                                                            <z:for i="0" while=<%= "i > Model.Count" %> let="i -= 2">
                                                                <p>@i</p>
                                                            </z:for>
                                                        </zml>

            y = lp.ParseZml()
            z =
"@for (var i = 0; i > Model.Count; i -= 2)
{
  <p>@i</p>
}"

            Assert.AreEqual(y, z)
        End Sub

        <TestMethod>
        Sub TestForLoopSteps()
            Dim lp = <zml xmlns:z="zml">
                                                                <z:for i="0" to="10" step="2">
                                                                    <p>@i</p>
                                                                </z:for>
                                                            </zml>

            Dim y = lp.ParseZml()
            Dim z =
"@for (var i = 0; i <10 + 1; i += 2)
{
  <p>@i</p>
}"

            lp = <zml xmlns:z="zml">
                                                                    <z:for type="Integer" i="@Model.Count - 1" to="0" step="-1">
                                                                        <p>@i</p>
                                                                    </z:for>
                                                                </zml>

            y = lp.ParseZml()
            z =
"@for (int i = Model.Count - 1; i > -1; i--)
{
  <p>@i</p>
}"

            Assert.AreEqual(y, z)

            lp = <zml xmlns:z="zml">
                                                                    <z:for type="Byte" i="@Model.Count - 1" to="0" step="-2">
                                                                        <p>@i</p>
                                                                    </z:for>
                                                                </zml>

            y = lp.ParseZml()
            z =
"@for (byte i = Model.Count - 1; i > -1; i -= 2)
{
  <p>@i</p>
}"

            Assert.AreEqual(y, z)
        End Sub


        <TestMethod>
        Sub TestForEachLoop()
            Dim lp = <zml xmlns:z="zml">
                                                                        <z:foreach var="i" in='"abcd"'>
                                                                            <p>@i</p>
                                                                        </z:foreach>
                                                                    </zml>


            Dim y = lp.ParseZml()
            Dim z =
$"@foreach (var i in {Qt}abcd{Qt})
{{
  <p>@i</p>
}}"
            Assert.AreEqual(y, z)

            lp = <zml xmlns:z="zml">
                                                                        <z:foreach type="Integer" var="i" in='"abcd"'>
                                                                            <p>@i</p>
                                                                        </z:foreach>
                                                                    </zml>


            y = lp.ParseZml()
            z =
$"@foreach (int i in {Qt}abcd{Qt})
{{
  <p>@i</p>
}}"
            Assert.AreEqual(y, z)

            lp = <zml xmlns:z="zml">
                                                                        <z:foreach i="" in="''abcd''">
                                                                            <p>@i</p>
                                                                        </z:foreach>
                                                                    </zml>


            y = lp.ParseZml()
            z =
$"@foreach (var i in {Qt}abcd{Qt})
{{
  <p>@i</p>
}}"
            Assert.AreEqual(y, z)
        End Sub

        <TestMethod>
        Sub TestNestedForEachLoops()

            Dim lp = <zml xmlns:z="zml">
                                                                            <z:foreach var="country" in="Model.Countries">
                                                                                <h1>Country: @country</h1>
                                                                                <z:foreach var="city" in="country.Cities">
                                                                                    <p>City: @city</p>
                                                                                </z:foreach>
                                                                            </z:foreach>
                                                                        </zml>


            Dim y = lp.ParseZml()
            Dim z =
"@foreach (var country in Model.Countries)
{
  <h1>Country: @country</h1>
  @foreach (var city in country.Cities)
  {
    <p>City: @city</p>
  }
}"

            Assert.AreEqual(y, z)
        End Sub

        <TestMethod>
        Sub TestIf()

            Dim x =
                    <zml xmlns:z="zml">
                                                                                <z:if condition=<%= "a>3 and y<5" %>>
                                                                                    <p>a = 4</p>
                                                                                </z:if>
                                                                            </zml>

            Dim y = x.ParseZml().ToString()
            Dim z =
"@if (a>3 & y<5)
{
  <p>a = 4</p>
}"

            Assert.AreEqual(y, z)

        End Sub

        <TestMethod>
        Sub TestIfElse()
            Dim x =
                <zml xmlns:z="zml">
                                                                                        <z:if condition=<%= "a <> 3 andalso b == 5" %>>
                                                                                            <z:then>
                                                                                                <p>test 1</p>
                                                                                            </z:then>
                                                                                            <z:else>
                                                                                                <p>test 2</p>
                                                                                            </z:else>
                                                                                        </z:if>
                                                                                    </zml>

            Dim y = x.ParseZml().ToString()
            Dim z =
"@if (a != 3 && b == 5)
{
  <p>test 1</p>
}
else
{
  <p>test 2</p>
}"

            Assert.AreEqual(y, z)

        End Sub

        <TestMethod>
        Sub TestElseIfs()
            Dim x =
                <zml xmlns:z="zml">
                                                                                            <z:if condition=<%= "grade < 30" %>>
                                                                                                <z:then>
                                                                                                    <p>Very weak</p>
                                                                                                </z:then>
                                                                                                <z:elseif condition=<%= "grade < 50" %>>
                                                                                                    <p>Weak 2</p>
                                                                                                </z:elseif>
                                                                                                <z:elseif condition=<%= "grade < 65" %>>
                                                                                                    <p>Accepted</p>
                                                                                                </z:elseif>
                                                                                                <z:elseif condition=<%= "grade < 75" %>>
                                                                                                    <p>Good</p>
                                                                                                </z:elseif>
                                                                                                <z:elseif condition=<%= "grade < 85" %>>
                                                                                                    <p>Very Good</p>
                                                                                                </z:elseif>
                                                                                                <z:else>
                                                                                                    <p>Excellent</p>
                                                                                                </z:else>
                                                                                            </z:if>
                                                                                        </zml>

            Dim y = x.ParseZml().ToString()
            Dim z =
"@if (grade < 30)
{
  <p>Very weak</p>
}
else if (grade < 50)
{
  <p>Weak 2</p>
}
else if (grade < 65)
{
  <p>Accepted</p>
}
else if (grade < 75)
{
  <p>Good</p>
}
else if (grade < 85)
{
  <p>Very Good</p>
}
else
{
  <p>Excellent</p>
}"

            Assert.AreEqual(y, z)

        End Sub

        <TestMethod>
        Sub TestNestedIfs()
            Dim x = <zml xmlns:z="zml">
                        <z:if condition="@Model.Count = 0">
                            <z:then>
                                <z:if condition="Model.Test">
                                    <z:then>
                                        <p>Test</p>
                                    </z:then>
                                    <z:else>
                                        <p>Not Test</p>
                                    </z:else>
                                </z:if>
                            </z:then>
                            <z:else>
                                <h1>Show Items</h1>
                                <z:foreach var="item" in="Model">
                                    <z:if condition="item.Id mod 2 = 0">
                                        <z:then>
                                            <p class="EvenItems">item.Name</p>
                                        </z:then>
                                        <z:else>
                                            <p class="OddItems">item.Name</p>
                                        </z:else>
                                    </z:if>
                                </z:foreach>
                                <p>Done</p>
                            </z:else>
                        </z:if>
                    </zml>

            Dim y = x.ParseZml().ToString()
            Dim z =
"@if (Model.Count == 0)
{
  @if (Model.Test)
  {
    <p>Test</p>
  }
  else
  {
    <p>Not Test</p>
  }
}
else
{
  <h1>Show Items</h1>
  @foreach (var item in Model)
  {
    @if (item.Id % 2 == 0)
    {
      <p class=" + Qt + "EvenItems" + Qt + ">item.Name</p>
    }
    else
    {
      <p class=" + Qt + "OddItems" + Qt + ">item.Name</p>
    }
  }
  <p>Done</p>
}"

            Assert.AreEqual(y, z)

        End Sub

        <TestMethod>
        Sub TestAttr()
            Dim x = <zml xmlns:z="zml">
                        <input type="hidden" name="Items[''@i''].Key" value="@item.Id"/>
                        <input type="number" class="esh-basket-input" min="1" name="Items[''@i''].Value" value="@item.Quantity"/>
                    </zml>

            Dim y = x.ParseZml.ToString()
            Dim z =
$"<input type={Qt}hidden{Qt} name='Items[{Qt}@i{Qt}].Key' value={Qt}@item.Id{Qt} />
<input type={Qt}number{Qt} class={Qt}esh-basket-input{Qt} min={Qt}1{Qt} name='Items[{Qt}@i{Qt}].Value' value={Qt}@item.Quantity{Qt} />"

            Assert.AreEqual(y, z)
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
        Sub TestImports()
            Dim x = <zml xmlns:z="zml">
                                                                                                                                <z:imports>Microsoft.eShopWeb.Web</z:imports>
                                                                                                                            </zml>

            Dim y = x.ParseZml().ToString()
            Dim z = "@using Microsoft.eShopWeb.Web"
            Assert.AreEqual(y, z)

            x = <zml xmlns:z="zml">
                                                                                                                                <z:using ns="Microsoft.eShopWeb.Web.ViewModels.Manage"/>
                                                                                                                            </zml>

            y = x.ParseZml().ToString()
            z = "@using Microsoft.eShopWeb.Web.ViewModels.Manage"
            Assert.AreEqual(y, z)

            x = <zml xmlns:z="zml">
                    <z:using Microsoft.eShopWeb.Web.Pages=""
                        Microsoft.AspNetCore.Identity=""
                        Microsoft.eShopWeb.Infrastructure.Identity=""/>
                </zml>

            y = x.ParseZml().ToString()
            z =
"@using Microsoft.eShopWeb.Web.Pages
@using Microsoft.AspNetCore.Identity
@using Microsoft.eShopWeb.Infrastructure.Identity"

            Assert.AreEqual(y, z)
        End Sub

        <TestMethod>
        Sub TestNamespace()
            Dim x = <zml xmlns:z="zml">
                                                                                                                                    <z:namespace>Microsoft.eShopWeb.Web</z:namespace>
                                                                                                                                </zml>

            Dim y = x.ParseZml().ToString()
            Dim z = $"@namespace Microsoft.eShopWeb.Web"
            Assert.AreEqual(y, z)

            x = <zml xmlns:z="zml">
                                                                                                                                    <z:namespace ns="Microsoft.eShopWeb.Web.ViewModels.Manage"/>
                                                                                                                                </zml>

            y = x.ParseZml().ToString()
            z = $"@namespace Microsoft.eShopWeb.Web.ViewModels.Manage"
            Assert.AreEqual(y, z)

            x = <zml xmlns:z="zml">
                    <z:namespace Microsoft.eShopWeb.Web.Pages=""
                        Microsoft.AspNetCore.Identity=""
                        Microsoft.eShopWeb.Infrastructure.Identity=""/>
                </zml>

            y = x.ParseZml().ToString()
            z =
"@namespace Microsoft.eShopWeb.Web.Pages
@namespace Microsoft.AspNetCore.Identity
@namespace Microsoft.eShopWeb.Infrastructure.Identity"

            Assert.AreEqual(y, z)

        End Sub

        <TestMethod>
        Sub TestAllImports()
            Dim x =
$"<z:imports>Microsoft.eShopWeb.Web</z:imports>
                                                                                                                                    <z:imports ns={Qt}Microsoft.eShopWeb.Web.ViewModels{Qt} />
                                                                                                                                    <z:using>Microsoft.eShopWeb.Web.ViewModels.Account</z:using>
                                                                                                                                    <z:using ns={Qt}Microsoft.eShopWeb.Web.ViewModels.Manage{Qt} />
                                                                                                                                    <z:using Microsoft.eShopWeb.Web.Pages
       Microsoft.AspNetCore.Identity
       Microsoft.eShopWeb.Infrastructure.Identity />
                                                                                                                                    <z:namespace>Microsoft.eShopWeb.Web.Pages</z:namespace>
                                                                                                                                    <z:helpers Microsoft.AspNetCore.Mvc.TagHelpers={Qt}*{Qt}/>"

            Dim y = x.ToXml.ParseZml().ToString()

            Dim z =
"@using Microsoft.eShopWeb.Web
@using Microsoft.eShopWeb.Web.ViewModels
@using Microsoft.eShopWeb.Web.ViewModels.Account
@using Microsoft.eShopWeb.Web.ViewModels.Manage
@using Microsoft.eShopWeb.Web.Pages
@using Microsoft.AspNetCore.Identity
@using Microsoft.eShopWeb.Infrastructure.Identity
@namespace Microsoft.eShopWeb.Web.Pages
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers"

            Assert.AreEqual(y, z)
        End Sub

        <TestMethod>
        Sub TestHelperImports()
            Dim x = <zml xmlns:z="zml">
                                                                                                                                            <z:helpers add="*" ns="Microsoft.AspNetCore.Mvc.TagHelpers"/>
                                                                                                                                        </zml>

            Dim y = x.ParseZml.ToString()

            Dim z = "@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers"
            Assert.AreEqual(y, z)

            x = <zml xmlns:z="zml">
                                                                                                                                            <z:helpers ns="Microsoft.AspNetCore.Mvc.TagHelpers"/>
                                                                                                                                        </zml>

            y = x.ParseZml.ToString()
            Assert.AreEqual(y, z)

            x = <zml xmlns:z="zml">
                              <z:helpers add="*">Microsoft.AspNetCore.Mvc.TagHelpers</z:helpers>
                                                                                                                                        </zml>

            y = x.ParseZml.ToString()
            Assert.AreEqual(y, z)

            x = <zml xmlns:z="zml">
                                                                                                                                            <z:helpers>Microsoft.AspNetCore.Mvc.TagHelpers</z:helpers>
                                                                                                                                        </zml>

            y = x.ParseZml.ToString()
            Assert.AreEqual(y, z)

            x = <zml xmlns:z="zml">
                    <z:helpers Microsoft.AspNetCore.Mvc.TagHelpers="*"
                        MyHelpers="*"/>
                </zml>

            y = x.ParseZml.ToString()
            z =
"@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@addTagHelper *, MyHelpers"

            Assert.AreEqual(y, z)
        End Sub

        <TestMethod>
        Sub TestInvokes()
            Dim x = <zml xmlns:z="zml">
                        <z:invoke method="Foo">
                            <z:arg>3</z:arg>
                            <z:arg>'a'</z:arg>
                            <z:arg>Ali</z:arg>
                            <z:lambda m.type="var" return="m.Name"/>
                            <z:lambda n.type="Integer" return="n + 1"/>
                            <z:lambda x.type="int" y.type="int" return="x + y"/>
                            <z:lambda a="Double" b="Single" return="a + b"/>
                        </z:invoke>
                    </zml>

            Dim y = x.ParseZml.ToString()
            Dim z = $"@Foo(3, 'a', {Qt}Ali{Qt}, m => m.Name, (int n) => n + 1, (int x, int y) => x + y, (double a, float b) => a + b)"
            Assert.AreEqual(y, z)

            x = <zml xmlns:z="zml">
                    <z:invoke method="RenderSection">
                        <z:arg>Scripts</z:arg>
                        <z:arg name="required">false</z:arg>
                    </z:invoke>
                </zml>

            y = x.ParseZml.ToString()
            z = "@RenderSection('Scripts', required: false)".Replace(SnglQt, Qt)
            Assert.AreEqual(y, z)

            x = <zml xmlns:z="zml">
                    <z:await method="Foo">
                        <z:arg>3</z:arg>
                        <z:arg>'a'</z:arg>
                        <z:arg>Ali</z:arg>
                        <z:lambda m="" return="m.Name"/>
                        <z:lambda n.type="Integer" return="n + 1"/>
                        <z:lambda x.type="int" y.type="int" return="x + y"/>
                        <z:lambda a="Double" b="Single" return="a + b"/>
                    </z:await>
                </zml>

            y = x.ParseZml.ToString()
            z = "@{ await " & $"Foo(3, 'a', {Qt}Ali{Qt}, m => m.Name, (int n) => n + 1, (int x, int y) => x + y, (double a, float b) => a + b);" & " }"
            Assert.AreEqual(y, z)

        End Sub

        <TestMethod>
        Sub TestNestedInvokes()
            Dim x = <zml xmlns:z="zml">
                        <z:invoke method="Foo">
                            <z:arg>3</z:arg>
                            <z:arg>
                                <z:invoke method="RenderSection">
                                    <z:arg>Scripts</z:arg>
                                    <z:arg name="required">false</z:arg>
                                </z:invoke>
                            </z:arg>
                            <z:arg>Ali</z:arg>
                            <z:lambda m="var">
                                <z:invoke property="m.Name"/>
                            </z:lambda>
                            <z:lambda x.type="int" y.type="int">
                                <z:invoke method="Test">
                                    <z:arg>@x</z:arg>
                                    <z:arg>@y</z:arg>
                                    <z:lambda a="Double" b="Single" return="a + b"/>
                                </z:invoke>
                            </z:lambda>
                            <z:await method="Foo2">
                                <z:arg>false</z:arg>
                                <z:arg>Ali</z:arg>
                            </z:await>
                        </z:invoke>
                    </zml>

            Dim y = x.ParseZml.ToString()
            Dim z =
"@Foo(
3, 
RenderSection('Scripts', required: false), 
'Ali', 
m => m.Name, 
(int x, int y) => Test(x, y, (double a, float b) => a + b), 
await Foo2(false, 'Ali'))".Replace((SnglQt, Qt), (vbCrLf, ""))

            Assert.AreEqual(y, z)

        End Sub

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
        Sub TestDots()
            Dim x = <zml xmlns:z="zml">
                        <z:set object="hasExternalLogins">
                            <z:dot>
                                <z:await method="SignInManager.GetExternalAuthenticationSchemesAsync"/>
                                <z:invoke method="Any"/>
                            </z:dot>
                        </z:set>
                    </zml>

            Dim y = x.ParseZml.ToString()
            Dim z = "@{ hasExternalLogins = (await SignInManager.GetExternalAuthenticationSchemesAsync()).Any(); }"

            Assert.AreEqual(y, z)
        End Sub

    End Class
End Namespace

