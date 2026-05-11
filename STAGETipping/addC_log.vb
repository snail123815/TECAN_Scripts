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
        ' This script should run twice in the script
        ' One after all parameters prepared
        ' One at the end of the script, remember to set IS_SCRIPT_DONE to "True"
        '   before executing this script
        Dim IS_SCRIPT_DONE As Boolean = Host.GetVariable("IS_SCRIPT_DONE")
        Dim logFilePath As String = Host.GetVariable("logFilePath") ' should be set in the first script addC_makePlateMap.vb

		WriteLog(logFilePath, "")
        Dim plateMapFilePath As String = "C:\Users\Public\Documents\proteomics\STAGETipping_addC_plateMap.txt"
        If Not IS_SCRIPT_DONE Then

            Dim N_SAMPLES As Integer = Host.GetVariable("N_SAMPLES")
            Dim OFFSET_SAMPLES As Integer = Host.GetVariable("OFFSET_SAMPLES")
            Dim MAKE_MIX As Boolean = Host.GetVariable("MAKE_MIX")
            Dim mixWellFilePath As String = Host.GetVariable("FILE_MIXWELLS")
            Dim IS_MIXALL As Boolean = Host.GetVariable("IS_MIXALL")
            WriteLog(logFilePath, String.Format("Executing {0} at {1}", "addC_log.vb", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")))
            WriteLog(logFilePath, "Experiment: Proteomics STAGETipping, add solution C and Mix, preparation done at the beginning of the script")
            WriteLog(logFilePath, String.Format("Parameters: N_SAMPLES={0}, OFFSET_SAMPLES={1}", N_SAMPLES, OFFSET_SAMPLES))
            WriteLog(logFilePath, String.Format("Mix parameters: MAKE_MIX={0}, IS_MIXALL={1}, FILE_MIXWELLS={2}", MAKE_MIX, IS_MIXALL, mixWellFilePath))
            WriteLog(logFilePath, String.Format("Sample in the plate starts from {0}",OFFSET_SAMPLES+1))
            Using reader As New StreamReader(plateMapFilePath)
                WriteLog(logFilePath, "Initial plate map:")
                While Not reader.EndOfStream
                    WriteLog(logFilePath, reader.ReadLine())
                End While
            End Using

            Host.SetVariable("logFilePath", logFilePath)
            WriteLog(logFilePath, String.Format("Finished executing {0} at {1}", "addC_log.vb", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")))

        Else

            WriteLog(logFilePath, String.Format("Finished executing {0} at {1}", "STAGETipping_addC", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")))
            WriteLog(logFilePath, String.Format("Executing final logging {0} at {1}", "addC_log.vb", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")))
            Dim VOL_C as Double = Host.GetVariable("VOL_C")
            Dim TIME_SHAKE as Integer = Host.GetVariable("TIME_SHAKE")
            Dim RPM_SHAKE as Integer = Host.GetVariable("RPM_SHAKE")
            Dim MAKE_MIX As Boolean = Host.GetVariable("MAKE_MIX")
            Dim VOL_EACH_TO_MIX as Double = Host.GetVariable("VOL_EACH_TO_MIX")
            Dim nDryDispense as Integer = Host.GetVariable("nDryDispense")
            WriteLog(logFilePath, "Experiment: Proteomics STAGETipping, add solution C and Mix, final log at the end of the script")
            WriteLog(logFilePath, String.Format("Shake to mix at {0} RPM for {1}s",RPM_SHAKE*100,TIME_SHAKE))
            WriteLog(logFilePath, String.Format("Solution C added volume: {0:F1} uL",VOL_C))
            If MAKE_MIX Then
                WriteLog(logFilePath,
                    String.Format("The mix contains {0:F1} uL of each sample",VOL_EACH_TO_MIX))
                WriteLog(logFilePath,
                    String.Format("The first {0} dispenses in the mix is dispensed by 'dry dispense'",
                    nDryDispense))
                WriteLog(logFilePath, "The rest mix dispenses is dispensed by 'wet dispense'")
            End If
            WriteLog(logFilePath, String.Format("Finished executing {0} at {1}", "addC_log.vb", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")))

        End If
        WriteLog(logFilePath, "")

    End Sub
End Class