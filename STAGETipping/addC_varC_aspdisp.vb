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

	Private Sub WriteLog(filePath As String, message As String)
		Using writer As New StreamWriter(filePath, True)
			writer.WriteLine(String.Format("[{0}] {1}", DateTime.Now.ToString("HH:mm:ss"), message))
		End Using
	End Sub

    Public Sub Execute() Implements IScriptObject.Execute

        Dim logFilePath As String = Host.GetVariable("logFilePath") ' should be set in the first script addC_makePlateMap.vb

		WriteLog(logFilePath, "")
        WriteLog(logFilePath, String.Format("Executing {0} at {1}", "addC_log.vb", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")))

        Dim variableCPath As String = Host.GetVariable("FILE_VARC") ' volume of solution C for each well, prepared by the user before the run
        Dim sampleOffset As Integer = Host.GetVariable("OFFSET_SAMPLES")
        Dim nSamples As Integer = Host.GetVariable("N_SAMPLES")
        Dim nloops_add_c As Integer = Host.getVariable("nloops_add_c")
        Dim loopIndex As Integer = Host.GetVariable("i")
        WriteLog(logFilePath,
            String.Format(
                "Parameters: FILE_VARC={0}, OFFSET_SAMPLES={1}, N_SAMPLES={2}, nloops_add_c={3}, current loop index i={4}",
                variableCPath, sampleOffset, nSamples, nloops_add_c, loopIndex
            )
        )

        WriteLog(logFilePath, String.Format("Finished executing {0} at {1}", "addC_log.vb", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")))
        WriteLog(logFilePath, "")


    End Sub
End Class