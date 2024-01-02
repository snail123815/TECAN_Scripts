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
                "C:\Users\Public\Documents\logFiles\STAGETipping_addC\STAGETipping_addC_log_{0}.txt",
                currentDateAndTime
            )
            Host.SetVariable("logFilePath", logFilePath)
        Else
            logFilePath = Host.GetVariable("logFilePath")
        End If

        Dim plateMapFilePath As String = "C:\Users\Public\Documents\proteomics\STAGETipping_addC_plateMap.txt"

        Using writer As New StreamWriter(logFilePath, True)
            If Not IS_SCRIPT_DONE Then

                Dim N_SAMPLES As Integer = Host.GetVariable("N_SAMPLES")
                Dim OFFSET_SAMPLES As Integer = Host.GetVariable("OFFSET_SAMPLES")
                writer.WriteLine("Experiment: Proteomics STAGETipping, add solution C and Mix")
                writer.WriteLine(String.Format("Start date and time: {0}",currentDateAndTime))
                writer.WriteLine(String.Format("Total sample number: {0}",N_SAMPLES))
                writer.WriteLine(String.Format("Sample in the plate starts from {0}",OFFSET_SAMPLES+1))
                Host.SetVariable("logFilePath", logFilePath)

            Else

                writer.WriteLine(String.Format("Script finished successfully at {0}",currentDateAndTime))
                Dim VOL_C as Double = Host.GetVariable("VOL_C")
                Dim TIME_SHAKE as Integer = Host.GetVariable("TIME_SHAKE")
                Dim RPM_SHAKE as Integer = Host.GetVariable("RPM_SHAKE")
                Dim MAKE_MIX As Boolean = Host.GetVariable("MAKE_MIX")
                Dim mixWell As Integer = Host.GetVariable("mixWell")
                Dim VOL_EACH_TO_MIX as Double = Host.GetVariable("VOL_EACH_TO_MIX")
                Dim nDryDispense as Integer = Host.GetVariable("nDryDispense")
                writer.WriteLine(String.Format("Shake to mix at {0} RPM for {1}s",RPM_SHAKE*100,TIME_SHAKE))
                writer.WriteLine(String.Format("Solution C added volume: {0:F1} uL",VOL_C))
                If MAKE_MIX Then
                    writer.WriteLine(
                        String.Format("A mix is created in well {0}",mixWell))
                    writer.WriteLine(
                        String.Format("The mix contains {0:F1} uL of each sample",VOL_EACH_TO_MIX))
                    writer.WriteLine(
                        String.Format("The first {0} dispenses in the mix is dispensed by 'dry dispense'",
                        nDryDispense))
                    writer.WriteLine("The rest mix dispenses is dispensed by 'wet dispense'")
                End If
                writer.WriteLine("")
                writer.WriteLine("Following content is the plate map of this experiment")
                writer.WriteLine("========================")

                ' Add plate map to log file
                Using plateMap As New StreamReader(plateMapFilePath)
                    While Not plateMap.EndOfStream
                        writer.WriteLine(plateMap.ReadLine())
                    End While
                End Using

            End If
        End Using

    End Sub
End Class