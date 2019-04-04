Imports Microsoft.AspNetCore
Imports Microsoft.AspNetCore.Hosting
Imports Microsoft.Extensions.Hosting

Module Program
    Sub Main(args As String())
        CreateHostBuilder(args).Build().Run()
    End Sub

    Public Function CreateHostBuilder(args() As String) As IWebHostBuilder
        Return WebHost.CreateDefaultBuilder(args).UseStartup(Of Startup)()
    End Function
End Module
