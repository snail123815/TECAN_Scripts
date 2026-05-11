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
		Set (ByVal value As IScriptingHost)
		Host = value
		End Set
	End Property

	Private Sub WriteLog(filePath As String, message As String)
		Using writer As New StreamWriter(filePath, True)
			writer.WriteLine(String.Format("[{0}] {1}", DateTime.Now.ToString("HH:mm:ss"), message))
		End Using
	End Sub

	Public Sub Execute() Implements IScriptObject.Execute
		' This script should run once at the beginning of the main script to
		' prepare the PLATE MAP file
		
		' PLATE MAP file path.
		' Write once (in this script); read on every column
		' Read back in every column to calculate tips to use for that column.
		' "wellNo;sampleOrMix;typeText;colorCode"
		Dim plateMapFilePath As String = "C:\Users\Public\Documents\proteomics\STAGETipping_addC_plateMap.txt"

		' Log file path
		Dim currentDateAndTime As String = DateTime.Now.ToString("yyyyMMdd_HHmm")
		Dim logFilePath As String = String.Format(
                "C:\Users\Public\Documents\logFiles\STAGETipping_addC\STAGETipping_addC_log_{0}.txt",
                currentDateAndTime
            )
		Host.SetVariable("logFilePath", logFilePath)

		Dim mixWell_all As Integer = - 1 ' 0 indexed, also used as the location of the first group when not mixing all.
		
		Dim IS_MIXALL As Boolean = Host.GetVariable("IS_MIXALL")
		Dim N_SAMPLES As Integer = Host.GetVariable("N_SAMPLES")
		Dim OFFSET_SAMPLES As Integer = Host.GetVariable("OFFSET_SAMPLES")
		Dim MAKE_MIX As Boolean = Host.GetVariable("MAKE_MIX")
		' MIX WELL file path, prepared per run by the user, get from Fluent.
		' "wellNo\tmixGroup\trow\tcolumn" The last two columns are optional
		Dim mixWellFilePath As String = Host.GetVariable("FILE_MIXWELLS")

		' key: mix group name, value: bitwise location of that mix group
		Dim mixGroups = New Dictionary(Of String, List(Of Integer))()
		Dim mixGroupTargetWells As New Dictionary(Of String, Integer)()
		Dim mixGroupColors As New Dictionary(Of String, String)()
		Dim colorCodeList As String() = {"5555dd", "55dd55", "dd5555", "55dddd", "dd55dd", "dddd55", "555555", "dddddd" }

		WriteLog(logFilePath, "")
		WriteLog(logFilePath, String.Format("Executing {0} at {1}", "addC_makePlateMap.vb", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")))
		WriteLog(logFilePath, String.Format("Parameters: IS_MIXALL={0}, N_SAMPLES={1}, OFFSET_SAMPLES={2}, MAKE_MIX={3}, FILE_MIXWELLS={4}", IS_MIXALL, N_SAMPLES, OFFSET_SAMPLES, MAKE_MIX, mixWellFilePath))
		
		If MAKE_MIX Then
			' Calculate mix well location based on sample number and offset
			If OFFSET_SAMPLES + N_SAMPLES >= 96 Then
				' Disable mix well if sample number exceeds the plate capacity
				mixWell_all = - 1
			ElseIf OFFSET_SAMPLES + N_SAMPLES = 95 Then
				mixWell_all = 95
			Else
				mixWell_all = OFFSET_SAMPLES + N_SAMPLES + 1
			End If
			Host.SetVariable("mixWell_all", mixWell_all + 1) ' 1 indexed for Fluent
			If Not IS_MIXALL Then ' User defined mix groups, read from file
				Using parser As New StreamReader(mixWellFilePath)
					Dim line As String = parser.ReadLine() ' skip header
					for i As Integer = 0 to 95
						line = parser.ReadLine()
						If Not String.IsNullOrEmpty(line) Then
							Dim parts As String() = line.Split(vbTab)
							Dim wellNo As Integer = CInt(parts(0)) ' 1 indexed from file
							Dim mixGroup As String = parts(1)
							If String.IsNullOrWhiteSpace(mixGroup) Then
								Continue For
							End If
							If Not mixGroups.ContainsKey(mixGroup) Then
								mixGroups(mixGroup) = New List(Of Integer)()
							End If
							mixGroups(mixGroup).Add(wellNo)
						End If
					Next
				End Using
				Dim nMixGroups As Integer = mixGroups.Keys.Count
				Host.SetVariable("N_mixGroups", nMixGroups)
				Dim mixGroupKeyArray(nMixGroups - 1) As String
				Dim keyIndex As Integer = 0
				For Each k As String In mixGroups.Keys
					mixGroupKeyArray(keyIndex) = k
					keyIndex += 1
				Next
				WriteLog(logFilePath, String.Format("N_mixGroups={0}, groups: [{1}]", nMixGroups, String.Join(", ", mixGroupKeyArray)))

				' Assign colors for different mix groups, max 8 groups
				' 0 indexed list
				Dim groupIndex As Integer = 0
				For Each mixGroup As String In mixGroups.Keys
					mixGroupColors(mixGroup) = colorCodeList(groupIndex Mod colorCodeList.Length)
					mixGroupTargetWells(mixGroup) = groupIndex + mixWell_all
					groupIndex += 1
				Next
				For Each kvp As KeyValuePair(Of String, Integer) In mixGroupTargetWells
					WriteLog(logFilePath, String.Format("Mix group '{0}' -> target well {1} (1-indexed)", kvp.Key, kvp.Value + 1))
				Next

			End If
		End If
		WriteLog(logFilePath, String.Format("mixWell_all (0-indexed)={0}, guard passes={1}", mixWell_all, MAKE_MIX AndAlso mixWell_all >= 0))
		
		
		' Make result array
		Dim resultArray(96, 3) As String
		For index As Integer = 0 To 95
			resultArray(index, 0) = (index + 1).ToString() ' 1 indexed for Fluent
			If OFFSET_SAMPLES <= index AndAlso index < (OFFSET_SAMPLES + N_SAMPLES) Then
				resultArray(index, 1) = "S"
				resultArray(index, 2) = String.Format("Sample No. {0}", index - OFFSET_SAMPLES + 1)
				resultArray(index, 3) = "888888"
				Continue For
			End If
				resultArray(index, 1) = "-"
				resultArray(index, 2) = "-"
				resultArray(index, 3) = "CCCCCC"
		Next

		' Assign mix group info to result array
		If MAKE_MIX AndAlso mixWell_all >= 0 AndAlso Not IS_MIXALL Then
			For Each mixGroup As String In mixGroups.Keys
				For Each wellNo As Integer In mixGroups(mixGroup)
					Dim wn = OFFSET_SAMPLES + wellNo - 1 ' convert to 0 indexed for array
					resultArray(wn, 1) = "S" & mixGroup ' mark the sample well with its mix group
					Dim currentText As String = resultArray(wn, 2)
					If currentText = "-" Then
						' raise error if the mix group file contains wells outside of the sample range
						WriteLog(logFilePath, String.Format("Error: Mix group '{0}' contains well {1} which is outside of the sample range. Check the mix well file.", mixGroup, wellNo))
						currentText = String.Format("Error: part of mix group {0}", mixGroup)
					Else
						currentText = currentText & String.Format(", part of mix group: {0}", mixGroup)
					End If
					resultArray(wn, 2) = currentText
					resultArray(wn, 3) = mixGroupColors(mixGroup)
				Next
			Next
		End If

		' Assign mix well info to result array
		If MAKE_MIX AndAlso mixWell_all >= 0 Then
			If IS_MIXALL Then
				resultArray(mixWell_all, 1) = "M"
				resultArray(mixWell_all, 2) = "Mix of all"
				resultArray(mixWell_all, 3) = "5555dd"
			Else
				For Each mixGroup As String In mixGroupTargetWells.Keys
					resultArray(mixGroupTargetWells(mixGroup), 1) = "M" & mixGroup
					resultArray(mixGroupTargetWells(mixGroup), 2) = String.Format("Mix group: {0}", mixGroup)
					resultArray(mixGroupTargetWells(mixGroup), 3) = mixGroupColors(mixGroup)
				Next
			End If
		End If
		
		' Write to PLATE MAP file
		Using writer As New StreamWriter(plateMapFilePath)
			For index As Integer = 0 To 95
				writer.WriteLine(
				String.Format("{0};{1};{2};{3}",
				resultArray(index, 0),
				resultArray(index, 1),
				resultArray(index, 2),
				resultArray(index, 3)
				))
			Next
		End Using
		WriteLog(logFilePath, "Plate map written successfully.")
		WriteLog(logFilePath, String.Format("Finished executing {0} at {1}", "addC_makePlateMap.vb", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")))
		WriteLog(logFilePath, "")
		End Sub
	End Class