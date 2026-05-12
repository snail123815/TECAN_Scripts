Imports Microsoft.VisualBasic
Imports System
Imports Microsoft.VisualBasic.FileIO
Imports System.IO
Imports System.Collections.Generic
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

    Private Function ReadColumnVolumesAndTipSelections(
        variableCPath As String,
        sampleOffset As Integer,
        nSamples As Integer,
        logFilePath As String
    ) As Tuple(Of Single(,), Integer())

        Dim columnVolumes(11, 7) As Single
        Dim loadedSamples As Integer = 0
        Dim plateIndex As Integer = sampleOffset

        Using reader As New StreamReader(variableCPath)
            reader.ReadLine() ' skip header

            While Not reader.EndOfStream AndAlso plateIndex < 96
                Dim line As String = reader.ReadLine()
                Dim parts As String() = line.Split(vbTab)
                Dim value As Single = 0

                If parts.Length >= 1 Then
                    Single.TryParse(parts(0), value)
                End If

                Dim columnIndex As Integer = plateIndex \ 8
                Dim rowIndex As Integer = plateIndex Mod 8
                columnVolumes(columnIndex, rowIndex) = value

                plateIndex += 1
                loadedSamples += 1
            End While
        End Using

        If loadedSamples <> nSamples Then
            WriteLog(logFilePath,
                String.Format("Warning: Expected to read {0} samples for variable C, but read {1}. Check the input file and parameters.",
                    nSamples, loadedSamples
            ))
        End If

        Dim tipSelectionByColumn(11) As Integer
        For columnIndex As Integer = 0 To 11
            Dim columnFlags(7) As Boolean
            For rowIndex As Integer = 0 To 7
                columnFlags(rowIndex) = (columnVolumes(columnIndex, rowIndex) > 0)
            Next
            tipSelectionByColumn(columnIndex) = CalculateTipSelection(columnFlags)
        Next

        Return Tuple.Create(columnVolumes, tipSelectionByColumn)
    End Function

    Public Sub Execute() Implements IScriptObject.Execute

        Dim logFilePath As String = Host.GetVariable("logFilePath") ' should be set in the first script addC_makePlateMap.vb

		WriteLog(logFilePath, "")
        WriteLog(logFilePath, String.Format("Executing {0} at {1}", "addC_varC_readvols.vb", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")))

        Dim variableCPath As String = Host.GetVariable("FILE_VARC") ' volume of solution C for each well, prepared by the user before the run
        Dim sampleOffset As Integer = Host.GetVariable("OFFSET_SAMPLES")
        Dim nSamples As Integer = Host.GetVariable("N_SAMPLES")
        Dim nloops_add_c As Integer = 0

        ' Parse the variable C file and calculate tip selections
        Dim parsedData As Tuple(Of Single(,), Integer()) = ReadColumnVolumesAndTipSelections(variableCPath, sampleOffset, nSamples, logFilePath)
        Dim columnVolumes As Single(,) = parsedData.Item1
        Dim tipSelectionByColumn As Integer() = parsedData.Item2

        WriteLog(logFilePath, String.Format("Read variable C values from {0}:", variableCPath))
        For columnIndex As Integer = 0 To 11
            For rowIndex As Integer = 0 To 7
                Dim wellIndex As Integer = (columnIndex * 8) + rowIndex + 1
                WriteLog(logFilePath, String.Format("Well {0}: {1}", wellIndex, columnVolumes(columnIndex, rowIndex)))
            Next
        Next


        ' Count how many columns contain any sample volume
        For columnIndex As Integer = 0 To 11
            If tipSelectionByColumn(columnIndex) > 0 Then
                nloops_add_c += 1
            End If
        Next
        WriteLog(logFilePath, String.Format("Calculated nloops_add_c={0} based on variable C values", nloops_add_c))

        Host.SetVariable("nloops_add_c", nloops_add_c)

        WriteLog(logFilePath, String.Format("Finished executing {0} at {1}", "addC_varC_readvols.vb", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")))
        WriteLog(logFilePath, "")


    End Sub
End Class