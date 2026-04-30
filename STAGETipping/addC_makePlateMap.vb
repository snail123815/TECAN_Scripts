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
	
	Public Sub Execute() Implements IScriptObject.Execute
		' This script should run once at the beginning of the main script to
		' prepare the PLATE MAP file
		
		' PLATE MAP file path.
		' Write once (in this script); read on every column
		' Read back in every column to calculate tips to use for that column.
		' "wellNo;sampleOrMix;typeText;colorCode"
		Dim plateMapFilePath As String = "C:\Users\Public\Documents\proteomics\STAGETipping_addC_plateMap.txt"
		Dim mixWell_all As Integer = 0 ' also used as the location of the first group
		
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
		
		If MAKE_MIX Then
			' Calculate mix well location based on sample number and offset
			If OFFSET_SAMPLES + N_SAMPLES >= 96 Then
				' Disable mix well if sample number exceeds the plate capacity
				mixWell_all = 0
			ElseIf OFFSET_SAMPLES + N_SAMPLES = 95 Then
				mixWell_all = 96
			Else
				mixWell_all = OFFSET_SAMPLES + N_SAMPLES + 2
			End If
			Host.SetVariable("mixWell_all", mixWell_all)
			If Not IS_MIXALL Then ' User defined mix groups, read from file
				Using parser As New StreamReader(mixWellFilePath)
				Dim line As String = parser.ReadLine() ' skip header
				line = parser.ReadLine()
				If Not String.IsNullOrEmpty(line) Then
					Dim parts As String() = line.Split(vbTab)
					Dim wellNo As Integer = CInt(parts(0))
					Dim mixGroup As String = parts(1)
					If Not mixGroups.ContainsKey(mixGroup) Then
						mixGroups(mixGroup) = New List(Of Integer)()
					End If
					mixGroups(mixGroup).Add(wellNo)
				End If
				End Using
				Dim nMixGroups As Integer = mixGroups.Keys.Count
				Host.SetVariable("N_mixGroups", nMixGroups)

				' Assign colors for different mix groups, max 8 groups
                ' 0 indexed list
                Dim groupIndex As Integer = 0
                For Each mixGroup As String In mixGroups.Keys
                    mixGroupColors(mixGroup) = colorCodeList(groupIndex Mod colorCodeList.Length)
                    mixGroupTargetWells(mixGroup) = groupIndex + mixWell_all
                    groupIndex += 1
                Next

			End If
		End If
		
		
		' Make result array
		Dim resultArray(97, 3) As String
		For index As Integer = 1 To 96
			resultArray(index, 0) = index.ToString()
			If OFFSET_SAMPLES < index AndAlso index <= (OFFSET_SAMPLES + N_SAMPLES) Then
				resultArray(index, 1) = "S"
				resultArray(index, 2) = String.Format("Sample No. {0}", index - OFFSET_SAMPLES)
				resultArray(index, 3) = "55bb55"
                Continue For
            End If
            If MAKE_MIX Then
                If IS_MIXALL Then
                    If index = mixWell_all Then
                        resultArray(index, 1) = "M"
                        resultArray(index, 2) = "Mix of all"
                        resultArray(index, 3) = "5555dd"
                        Continue For
                    End If
                Else
                    Dim mixGroupFound As Boolean = False
                    For Each mixGroup As String In mixGroupTargetWells.Keys
                        If mixGroupTargetWells(mixGroup) = index Then
                            resultArray(index, 1) = "M" & mixGroup
                            resultArray(index, 2) = String.Format("Mix group: {0}", mixGroup)
                            resultArray(index, 3) = mixGroupColors(mixGroup)
                            mixGroupFound = True
                            Exit For
                        End If
                    Next
                    If mixGroupFound Then
                        Continue For
                    End If
                End If
            End If
            resultArray(index, 1) = "-"
            resultArray(index, 2) = "-"
            resultArray(index, 3) = "555555"
		Next
		
		' Write to PLATE MAP file
		Using writer As New StreamWriter(plateMapFilePath)
		For index As Integer = 1 To 96
			writer.WriteLine(
			String.Format("{0};{1};{2};{3}",
			resultArray(index, 0),
			resultArray(index, 1),
			resultArray(index, 2),
			resultArray(index, 3)
			))
		Next
		End Using
		
	End Sub
End Class