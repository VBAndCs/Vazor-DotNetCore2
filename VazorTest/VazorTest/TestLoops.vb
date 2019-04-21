Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Vazor

<TestClass>
Public Class TestLoops

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
        Assert.AreEqual(y, z)


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
  foreach (var city in country.Cities)
  {
    <p>City: @city</p>
  }
}"

        Assert.AreEqual(y, z)
    End Sub

End Class
