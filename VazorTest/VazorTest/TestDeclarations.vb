Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Vazor

<TestClass>
Public Class TestDeclarations

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
    Sub TestQuotesInAttr()
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

End Class
