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
        ' This script should run twice in the script
        ' One after all parameters prepared
        ' One at the end of the script, remember to set IS_SCRIPT_DONE to "True"
        '   before executing this script
        Dim IS_SCRIPT_DONE As Boolean = Host.GetVariable("IS_SCRIPT_DONE")
        Dim currentDateAndTime As String = DateTime.Now.ToString("yyyyMMdd_HHmm")
        Dim logFilePath As String
        If Not IS_SCRIPT_DONE Then
            logFilePath = String.Format(
                "C:\Users\Public\Documents\logFiles\proteomics_sample_prep\proteomics_main_{0}.txt",
                currentDateAndTime
            )
            Host.SetVariable("logFilePath", logFilePath)
        Else
            logFilePath = Host.GetVariable("logFilePath")
        End If

        Using writer As New StreamWriter(logFilePath, True)
            If Not IS_SCRIPT_DONE Then

                Dim N_SAMPLES As Integer = Host.GetVariable("N_SAMPLES")
                Dim OFFSET_SAMPLES As Integer = Host.GetVariable("OFFSET_SAMPLES")
                writer.WriteLine("Experiment: Proteomics sample prepare, main script")
                writer.WriteLine(String.Format("Start date and time: {0}",currentDateAndTime))
                writer.WriteLine(String.Format("Total sample number: {0}",N_SAMPLES))

                Host.SetVariable("logFilePath", logFilePath)

            Else

                writer.WriteLine(String.Format("Script finished successfully at {0}",currentDateAndTime))
                writer.WriteLine("")
                writer.WriteLine("========================")

            End If
        End Using

    End Sub
End Class