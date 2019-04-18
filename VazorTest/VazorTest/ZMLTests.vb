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
            Dim x = <zml>
                        <p asp-a="students"
                            asp-b="@students"
                            asp-c="Model.students"
                            asp-d="@Model.students"
                        />
                    </zml>

            Dim p As XElement = x.ParseZml().ToXml().FirstNode
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
        Sub TestComments()
            Dim x =
                <zml>
                    <p>test</p>
                    <comment>comment</comment>
                    <comment>
                        <p>test comment</p>
                    </comment>
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
            Dim x = <zml>
                                <declare
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

            x = <zml><declare var="arr" value="new String(){}"/></zml>
            Dim z = x.ParseZml()
            Assert.AreEqual(z, $"@{{ var arr = {Qt}new String(){{}}{Qt}; }}")

            x = <zml><declare var="arr" value="@new String[]{}"/></zml>
            z = x.ParseZml()
            Assert.AreEqual(z, $"@{{ var arr = new String[]{{}}; }}")

            x = <zml><declare var="Name" key="Adam">dict</declare></zml>
            z = x.ParseZml()
            Assert.AreEqual(z, $"@{{ var Name = dict[{Qt}Adam{Qt}]; }}")

            x = <zml><declare var="Name" value="dict" key="Adam"/></zml>
            z = x.ParseZml()
            Assert.AreEqual(z, $"@{{ var Name = dict[{Qt}Adam{Qt}]; }}")

            x = <zml><declare var="Name" key="@Adam">@dict</declare></zml>
            z = x.ParseZml()
            Assert.AreEqual(z, $"@{{ var Name = dict[Adam]; }}")

            ' Test Types
            ' ------------------------
            x = <zml><declare var="arr" type="int" value="new String(){}"/></zml>
            z = x.ParseZml()
            Assert.AreEqual(z, $"@{{ int arr = {Qt}new String(){{}}{Qt}; }}")

            x = <zml><declare var="arr" type="Int32" value="@new String[]{}"/></zml>
            z = x.ParseZml()
            Assert.AreEqual(z, $"@{{ Int32 arr = new String[]{{}}; }}")

            x = <zml><declare var="Name" type="Integer" key="Adam">dict</declare></zml>
            z = x.ParseZml()
            Assert.AreEqual(z, $"@{{ int Name = dict[{Qt}Adam{Qt}]; }}")

            x = <zml><declare var="Name" type="Long" value="dict" key="Adam"/></zml>
            z = x.ParseZml()
            Assert.AreEqual(z, $"@{{ long Name = dict[{Qt}Adam{Qt}]; }}")

            x = <zml><declare var="Name" type="List(Of Single, UInteger)" key="@Adam">@dict</declare></zml>
            z = x.ParseZml()
            Assert.AreEqual(z, $"@{{ List<float, uint> Name = dict[Adam]; }}")

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

            x = <zml><set object="dict" key="Name">Adam</set></zml>
            z = x.ParseZml()
            Assert.AreEqual(z, $"@{{ dict[{Qt}Name{Qt}] = {Qt}Adam{Qt}; }}")

            x = <zml><set object="dict" key="Name" value="Adam"/></zml>
            z = x.ParseZml()
            Assert.AreEqual(z, $"@{{ dict[{Qt}Name{Qt}] = {Qt}Adam{Qt}; }}")

            x = <zml><set object="dict" key="@Name">@Adam</set></zml>
            z = x.ParseZml()
            Assert.AreEqual(z, $"@{{ dict[Name] = Adam; }}")

        End Sub

        <TestMethod>
        Sub TestGetters()
            Dim x = <zml><get>X</get></zml>
            Dim y = x.ParseZml().ToXml()
            Dim z = y.Value.Trim()
            Assert.AreEqual(z, "@X")

            x = <zml><get object="X"/></zml>
            y = x.ParseZml().ToXml()
            z = y.Value.Trim()
            Assert.AreEqual(z, "@X")

            x = <zml><get object="X" key="@i"/></zml>
            y = x.ParseZml().ToXml()
            z = y.Value.Trim()
            Assert.AreEqual(z, "@X[i]")

            x = <zml><get object="X" key="name"/></zml>
            y = x.ParseZml().ToXml()
            z = y.Value.Trim()
            Assert.AreEqual(z, $"@X[{Qt}name{Qt}]")

        End Sub

        <TestMethod>
        Sub TestPage()
            Dim x = <zml><page/></zml>
            Dim y = x.ParseZml().ToXml()
            Dim z = y.Value.Trim()
            Assert.AreEqual(z, "@page")

            x = <zml><page>Pages/Home</page></zml>
            y = x.ParseZml().ToXml()
            z = y.Value.Trim()
            Assert.AreEqual(z, $"@page {Qt}Pages/Home{Qt}")

            x = <zml><page route="Pages/Home"/></zml>
            y = x.ParseZml().ToXml()
            z = y.Value.Trim()
            Assert.AreEqual(z, $"@page {Qt}Pages/Home{Qt}")

        End Sub

        <TestMethod>
        Sub TestModel()
            Dim x = <zml><model>IndexModel</model></zml>
            Dim y = x.ParseZml()
            Assert.IsTrue(y.Contains("@model IndexModel"))

            x = <zml><model type="IndexModel"/></zml>
            y = x.ParseZml()
            Assert.IsTrue(y.Contains("@model IndexModel"))

        End Sub

        <TestMethod>
        Sub TestForLoop()
            Dim lp =
                <zml>
                    <for i="0" to="10">
                        <p>@i</p>
                    </for>
                </zml>

            Dim y = lp.ParseZml()
            Dim z =
"@for (var i = 0; i <10 + 1; i++)
{
  <p>@i</p>
}"

            lp = <zml>
                     <for i="0" to="@Model.Count - 1">
                         <p>@i</p>
                     </for>
                 </zml>

            y = lp.ParseZml()
            z =
"@for (var i = 0; i < Model.Count; i++)
{
  <p>@i</p>
}"

            Assert.AreEqual(y, z)

            lp = <zml>
                     <for i="0" while=<%= "i < Model.Count" %> let="i++">
                         <p>@i</p>
                     </for>
                 </zml>

            y = lp.ParseZml()
            z =
"@for (var i = 0; i < Model.Count; i++)
{
  <p>@i</p>
}"

            Assert.AreEqual(y, z)

            lp = <zml>
                     <for i="0" while=<%= "i > Model.Count" %> let="i -= 2">
                         <p>@i</p>
                     </for>
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
            Dim lp = <zml>
                         <for i="0" to="10" step="2">
                             <p>@i</p>
                         </for>
                     </zml>

            Dim y = lp.ParseZml()
            Dim z =
"@for (var i = 0; i <10 + 1; i += 2)
{
  <p>@i</p>
}"

            lp = <zml>
                     <for type="Integer" i="@Model.Count - 1" to="0" step="-1">
                         <p>@i</p>
                     </for>
                 </zml>

            y = lp.ParseZml()
            z =
"@for (int i = Model.Count - 1; i > -1; i--)
{
  <p>@i</p>
}"

            Assert.AreEqual(y, z)

            lp = <zml>
                     <for type="Byte" i="@Model.Count - 1" to="0" step="-2">
                         <p>@i</p>
                     </for>
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
            Dim lp = <zml>
                         <foreach var="i" in='"abcd"'>
                             <p>@i</p>
                         </foreach>
                     </zml>


            Dim y = lp.ParseZml()
            Dim z =
$"@foreach (var i in {Qt}abcd{Qt})
{{
  <p>@i</p>
}}"
            Assert.AreEqual(y, z)

            lp = <zml>
                     <foreach type="Integer" var="i" in='"abcd"'>
                         <p>@i</p>
                     </foreach>
                 </zml>


            y = lp.ParseZml()
            z =
$"@foreach (int i in {Qt}abcd{Qt})
{{
  <p>@i</p>
}}"
            Assert.AreEqual(y, z)

            lp = <zml>
                     <foreach i="" in="''abcd''">
                         <p>@i</p>
                     </foreach>
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

            Dim lp = <zml>
                         <foreach var="country" in="Model.Countries">
                             <h1>Country: @country</h1>
                             <foreach var="city" in="country.Cities">
                                 <p>City: @city</p>
                             </foreach>
                         </foreach>
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
                    <zml>
                        <if condition=<%= "a>3 and y<5" %>>
                            <p>a = 4</p>
                        </if>
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
                <zml>
                    <if condition=<%= "a <> 3 andalso b == 5" %>>
                        <then>
                            <p>test 1</p>
                        </then>
                        <else>
                            <p>test 2</p>
                        </else>
                    </if>
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
                <zml>
                    <if condition=<%= "grade < 30" %>>
                        <then>
                            <p>Very weak</p>
                        </then>
                        <elseif condition=<%= "grade < 50" %>>
                            <p>Weak 2</p>
                        </elseif>
                        <elseif condition=<%= "grade < 65" %>>
                            <p>Accepted</p>
                        </elseif>
                        <elseif condition=<%= "grade < 75" %>>
                            <p>Good</p>
                        </elseif>
                        <elseif condition=<%= "grade < 85" %>>
                            <p>Very Good</p>
                        </elseif>
                        <else>
                            <p>Excellent</p>
                        </else>
                    </if>
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
            Dim x = <zml>
                        <if condition="@Model.Count = 0">
                            <then>
                                <if condition="Model.Test">
                                    <then>
                                        <p>Test</p>
                                    </then>
                                    <else>
                                        <p>Not Test</p>
                                    </else>
                                </if>
                            </then>
                            <else>
                                <h1>Show Items</h1>
                                <foreach var="item" in="Model">
                                    <if condition="item.Id mod 2 = 0">
                                        <then>
                                            <p class="EvenItems">item.Name</p>
                                        </then>
                                        <else>
                                            <p class="OddItems">item.Name</p>
                                        </else>
                                    </if>
                                </foreach>
                                <p>Done</p>
                            </else>
                        </if>
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
            Dim x = <zml>
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
            Dim x = <zml>
                        <layout>_Layout</layout>
                    </zml>

            Dim y = x.ParseZml.ToString()
            Dim z =
"@{
    Layout = '_Layout';
}".Replace(SnglQt, Qt)

            Assert.AreEqual(y, z)

            x = <zml>
                    <layout page="_Layout"/>
                </zml>

            y = x.ParseZml.ToString()

            Assert.AreEqual(y, z)
        End Sub

        <TestMethod>
        Sub TestImports()
            Dim x = <zml>
                        <imports>Microsoft.eShopWeb.Web</imports>
                    </zml>

            Dim y = x.ParseZml().ToString()
            Dim z = "@using Microsoft.eShopWeb.Web"
            Assert.AreEqual(y, z)

            x = <zml>
                    <using ns="Microsoft.eShopWeb.Web.ViewModels.Manage"/>
                </zml>

            y = x.ParseZml().ToString()
            z = "@using Microsoft.eShopWeb.Web.ViewModels.Manage"
            Assert.AreEqual(y, z)

            x = <zml>
                    <using Microsoft.eShopWeb.Web.Pages=""
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
            Dim x = <zml>
                        <namespace>Microsoft.eShopWeb.Web</namespace>
                    </zml>

            Dim y = x.ParseZml().ToString()
            Dim z = $"@namespace Microsoft.eShopWeb.Web"
            Assert.AreEqual(y, z)

            x = <zml>
                    <namespace ns="Microsoft.eShopWeb.Web.ViewModels.Manage"/>
                </zml>

            y = x.ParseZml().ToString()
            z = $"@namespace Microsoft.eShopWeb.Web.ViewModels.Manage"
            Assert.AreEqual(y, z)

            x = <zml>
                    <namespace Microsoft.eShopWeb.Web.Pages=""
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
$"<imports>Microsoft.eShopWeb.Web</imports>
<imports ns={Qt}Microsoft.eShopWeb.Web.ViewModels{Qt} />
<using>Microsoft.eShopWeb.Web.ViewModels.Account</using>
<using ns={Qt}Microsoft.eShopWeb.Web.ViewModels.Manage{Qt} />
<using Microsoft.eShopWeb.Web.Pages
       Microsoft.AspNetCore.Identity
       Microsoft.eShopWeb.Infrastructure.Identity />
<namespace>Microsoft.eShopWeb.Web.Pages</namespace>
<helpers Microsoft.AspNetCore.Mvc.TagHelpers={Qt}*{Qt}/>"

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
            Dim x = <zml>
                        <helpers add="*" ns="Microsoft.AspNetCore.Mvc.TagHelpers"/>
                    </zml>

            Dim y = x.ParseZml.ToString()

            Dim z = "@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers"
            Assert.AreEqual(y, z)

            x = <zml>
                    <helpers ns="Microsoft.AspNetCore.Mvc.TagHelpers"/>
                </zml>

            y = x.ParseZml.ToString()
            Assert.AreEqual(y, z)

            x = <zml>
                    <helpers add="*">Microsoft.AspNetCore.Mvc.TagHelpers</helpers>
                </zml>

            y = x.ParseZml.ToString()
            Assert.AreEqual(y, z)

            x = <zml>
                    <helpers>Microsoft.AspNetCore.Mvc.TagHelpers</helpers>
                </zml>

            y = x.ParseZml.ToString()
            Assert.AreEqual(y, z)

            x = <zml>
                    <helpers Microsoft.AspNetCore.Mvc.TagHelpers="*"
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
            Dim x = <zml>
                        <invoke method="Foo">
                            <arg>3</arg>
                            <arg>'a'</arg>
                            <arg>Ali</arg>
                            <m return="m.Name"/>
                            <n type="Integer" return="n + 1"/>
                            <lambda x.type="int" y.type="int" return="x + y"/>
                            <lambda a="Double" b="Single" return="a + b"/>
                        </invoke>
                    </zml>

            Dim y = x.ParseZml.ToString()
            Dim z = $"@Foo(3, 'a', {Qt}Ali{Qt}, m => m.Name, (int n) => n + 1, (int x, int y) => x + y, (double a, float b) => a + b)"

            Assert.AreEqual(y, z)
        End Sub

        <TestMethod>
        Sub TestSections()
            Dim x = <zml>
                        <section name="Scripts">
                            <partial name="_ValidationScriptsPartial"/>
                        </section>
                    </zml>

            Dim y = x.ParseZml.ToString()
            Dim z =
"@section Scripts
{
  <partial name='_ValidationScriptsPartial' />
}".Replace(SnglQt, Qt)

            Assert.AreEqual(y, z)
        End Sub

    End Class
End Namespace

