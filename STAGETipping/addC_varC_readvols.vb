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

    Private Function CalculateTipSelection(flags As Boolean()) As Integer
        If flags Is Nothing OrElse flags.Length <> 8 Then
            Throw New ArgumentException("flags must contain exactly 8 boolean values.")
        End If

        Dim result As Integer = 0
        For i As Integer = 0 To 7
            If flags(i) Then
                result += (1 << i)
            End If
        Next

        Return result
    End Function

    Public Sub Execute() Implements IScriptObject.Execute

        Dim logFilePath As String = Host.GetVariable("logFilePath") ' should be set in the first script addC_makePlateMap.vb

		WriteLog(logFilePath, "")
        WriteLog(logFilePath, String.Format("Executing {0} at {1}", "addC_log.vb", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")))

        Dim variableCPath As String = Host.GetVariable("FILE_VARC") ' volume of solution C for each well, prepared by the user before the run
        Dim sampleOffset As Integer = Host.GetVariable("OFFSET_SAMPLES")
        Dim nSamples As Integer = Host.GetVariable("N_SAMPLES")
        Dim nloops_add_c As Integer = 0
        Dim variableCValues(95) As Single

        ' Read file into array of 96 elements
        ' # Start from first sample, ignore offset	
        ' 130
        ' 130
        ' 79.28085106
        ' 130
        ' 130
        Using reader As New StreamReader(variableCPath)
            Dim index As Integer = sampleOffset ' start from the first sample, 0 indexed
            Dim seenSampleCount As Integer = 0
            reader.ReadLine() ' skip header
            While Not reader.EndOfStream And index < 96
                Dim line As String = reader.ReadLine()
                Dim parts As String() = line.Split(vbTab)
                If parts.Length >= 1 Then
                    Single.TryParse(parts(0), variableCValues(index)) ' assuming the first column has the volume values
                End If
                index += 1
            End While
        End Using

        WriteLog(logFilePath, String.Format("Read variable C values from {0}:", variableCPath))
        For i As Integer = 0 To 95
            WriteLog(logFilePath, String.Format("Well {0}: {1}", i + 1, variableCValues(i)))
        Next


        ' Loop through 12 columns, if this column has samples, then add a loop
    
        Dim tipSelection As Integer = 0
        For currentColumn As Integer = 1 To 12
            Dim columnSum As Integer = 0
            Dim columnStart As Integer = (currentColumn - 1) * 8
            For rowOffset As Integer = 0 To 7
                columnSum += variableCValues(columnStart + rowOffset)
            Next

            If columnSum > 0 Then ' if there is any sample in this column
                nloops_add_c += 1
                Dim columnFlags(7) As Boolean
                For i As Integer = 0 To 7
                    columnFlags(i) = (variableCValues(columnStart + i) > 0)
                Next
                tipSelection = Math.Max(tipSelection, CalculateTipSelection(columnFlags))
            End If
        Next
        WriteLog(logFilePath, String.Format("Calculated nloops_add_c={0} based on variable C values", nloops_add_c))

        Host.SetVariable("nloops_add_c", nloops_add_c)

        WriteLog(logFilePath, String.Format("Finished executing {0} at {1}", "addC_log.vb", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")))
        WriteLog(logFilePath, "")


    End Sub
End Class