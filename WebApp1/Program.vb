Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Threading.Tasks
Imports Microsoft.AspNetCore
Imports Microsoft.AspNetCore.Hosting
Imports Microsoft.Extensions.Configuration
Imports Microsoft.Extensions.Hosting
Imports Microsoft.Extensions.Logging

Module Program
    Sub Main(args As String())
        CreateHostBuilder(args).Build().Run()
    End Sub

    Public Function CreateHostBuilder(args() As String) As IWebHostBuilder
        Return WebHost.CreateDefaultBuilder(args).UseStartup(Of Startup)()
    End Function
End Module
