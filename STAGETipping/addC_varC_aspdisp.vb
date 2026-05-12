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
        WriteLog(logFilePath, String.Format("Executing {0} at {1}", "addC_varC_aspdisp.vb", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")))

        Dim variableCPath As String = Host.GetVariable("FILE_VARC") ' volume of solution C for each well, prepared by the user before the run
        Dim variableCPathMachine As String = Path.Combine(Path.GetDirectoryName(variableCPath),
        Path.GetFileNameWithoutExtension(variableCPath) & "_machine" & Path.GetExtension(variableCPath))
        Dim sampleOffset As Integer = Host.GetVariable("OFFSET_SAMPLES")
        Dim nSamples As Integer = Host.GetVariable("N_SAMPLES")
        Dim nloops_add_c As Integer = Host.GetVariable("nloops_add_c")
        Dim loopIndex As Integer = Host.GetVariable("i") ' 1 indexed
        Dim tipSelection As Integer
        Dim aspvs(7) As Single
        Dim dispWelloffset As Integer

        WriteLog(logFilePath,
            String.Format(
                "Parameters: current loop index i={4}, FILE_VARC={0}, OFFSET_SAMPLES={1}, N_SAMPLES={2}, nloops_add_c={3}",
                variableCPath, sampleOffset, nSamples, nloops_add_c, loopIndex
            )
        )

        Dim parsedData As Tuple(Of Single(,), Integer()) = ReadColumnVolumesAndTipSelections(variableCPath, sampleOffset, nSamples, logFilePath)
        Dim columnVolumes As Single(,) = parsedData.Item1
        Dim tipSelectionByColumn As Integer() = parsedData.Item2

        Dim columnsToAdd As New List(Of Integer)()
        Dim ts As Integer
        For i As Integer = 0 To 11
            ts = tipSelectionByColumn(i)
            If ts > 0 Then
                columnsToAdd.Add(i)
            End If
        Next

        If loopIndex < 1 OrElse loopIndex > columnsToAdd.Count Then
            Throw New ArgumentOutOfRangeException("i",
                String.Format("Loop index i={0} is out of range for {1} active columns.", loopIndex, columnsToAdd.Count))
        End If

        Dim selectedColumn As Integer = columnsToAdd(loopIndex - 1)
        tipSelection = tipSelectionByColumn(selectedColumn)
        dispWelloffset = selectedColumn * 8
        For j As Integer = 0 To 7
            aspvs(j) = columnVolumes(selectedColumn, j)
        Next

        WriteLog(logFilePath, String.Format("Column {0}", selectedColumn))
        WriteLog(logFilePath, String.Format("Tip selection {0}", tipSelection))
        WriteLog(logFilePath, String.Format("Columns with variable C to add: {0}", String.Join(", ", columnsToAdd)))
        WriteLog(logFilePath, String.Format("Aspiration volumes: {0}", String.Join(", ", aspvs)))
        WriteLog(logFilePath, String.Format("Dispense well offset {0}", dispWelloffset))


        ' Done, write variables back to the main script
        Host.SetVariable("tip_selection", tipSelection)
        Host.SetVariable("disp_welloffset", dispWelloffset)
        For i As Integer = 0 To 7
            Host.SetVariable(String.Format("aspv{0}", i+1), aspvs(i))
        Next

        WriteLog(logFilePath, String.Format("Finished executing {0} at {1}", "addC_varC_aspdisp.vb", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")))
        WriteLog(logFilePath, "")


    End Sub
End Class