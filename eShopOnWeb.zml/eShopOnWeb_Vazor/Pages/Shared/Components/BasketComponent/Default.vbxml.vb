' To have html editor support:
'     1. Close this file.
'     2. Right clicke On "Page.vbxml.vb" in solution explorer.
'     3. Click "Open with" from the context menu.
'     4. Choose "Html Editor" and click Ok.
' To go back to edit vb code, close the file, and double click on
' "Page.vbxml.vb" in solution explorer.

Imports Microsoft.eShopWeb.Web.ViewModels

Namespace Pages.[Shared].Components.BasketComponent
    Partial Public Class Basket

        Protected Function GetVbXml(model As BasketComponentViewModel) As XElement
            Return _
 _
<a class="esh-basketstatus "
    asp-page="/Basket/Index">
    <div class="esh-basketstatus-image">
        <img src="~/images/cart.png"/>
    </div>
    <div class="esh-basketstatus-badge">
        <%= model.ItemsCount %>
    </div>
</a>

        End Function


    End Class
End Namespace