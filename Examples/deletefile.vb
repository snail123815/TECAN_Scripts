Imports Microsoft.VisualBasic
Imports System
Imports Tecan.Core.Scripting

Public Class TestClass
    Implements IScriptObject

    Private Host As IScriptingHost

    Public Property ScriptingHost() As IScriptingHost Implements IScriptObject.ScriptingHost
        Get
            Return Host
        End Get
        Set(ByVal value As IScriptingHost)
            Host = value
        End Set
    End Property

    Public Sub Execute() Implements IScriptObject.Execute
        My.Computer.FileSystem.DeleteFile("C:\Users\Public\Documents\vbScripts\GetAndSetNumVars.vb")
        Dim factor1 As Double
         ' Dim result As Double
         ' factor1 = Host.GetVariable("i")
         ' result = factor1 * 10.32

         ' Host.SetVariable("result", result)
    End Sub
End Class