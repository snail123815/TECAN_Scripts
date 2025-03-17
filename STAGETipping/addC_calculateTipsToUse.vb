Imports Microsoft.VisualBasic
Imports System
Imports Microsoft.VisualBasic.FileIO
Imports System.IO
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
        Dim plateMapFilePath As String = "C:\Users\Public\Documents\proteomics\STAGETipping_addC_plateMap.txt"

        Dim MAKE_MIX As Boolean = Host.GetVariable("MAKE_MIX")
        Dim LOOP_I As Integer = Host.GetVariable("i")
        Dim SKIP_LINES As Integer = (LOOP_I-1)*8
        Dim bitTipsNeeded As Integer = 0
        Dim bitTipsDryDispense As Integer = 0
        Dim bitTipsWetDispense As Integer = 0
        Dim nDryDispense As Integer = Host.GetVariable("nDryDispense")

        Using parser As New StreamReader(plateMapFilePath)
            For i as Integer = 1 To SKIP_LINES
                Dim s As String = parser.ReadLine().Split(";"c)(1)
                If s = "S" AndAlso nDryDispense > 0 Then
                    nDryDispense -= 1
                End If
            Next

            For i as Integer = 1 to 8
                Dim s As String = parser.ReadLine().Split(";"c)(1)
                Dim bitTip As Integer = CInt(Math.Pow(2, i-1))
                If s = "S" Then
                    bitTipsNeeded += bitTip
                    If nDryDispense > 0 Then
                        bitTipsDryDispense += bitTip
                        nDryDispense -= 1
                    Else
                        bitTipsWetDispense += bitTip
                    End If
                End If
            Next
        End Using

        Host.SetVariable("bitTipsNeeded", bitTipsNeeded)
        Host.SetVariable("bitTipsWetDispense", bitTipsWetDispense)
        Host.SetVariable("bitTipsDryDispense", bitTipsDryDispense)
        Host.SetVariable("nDryDispense", nDryDispense)

    End Sub
End Class