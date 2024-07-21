Imports Microsoft.VisualBasic
Imports System
Imports Tecan.Core.Scripting
Imports Microsoft.VisualBasic.FileIO
Imports System.IO
Imports System.Text.RegularExpressions


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

    Public Function row_letter_to_number(column As String) As Integer
        Dim result As Integer = 0
        Dim multiplier As Integer = 1
        For i As Integer = column.Length - 1 To 0 Step -1
            Dim charValue As Integer = Asc(column(i).ToString().ToUpper()) - Asc("A"c) + 1
            result += charValue * multiplier
            multiplier *= 26
        Next
        Return result
    End Function

    Public Function string_row_column(input As String) As Tuple(Of Integer, Integer)
        Dim regex As New Regex("([A-Za-z]+)([0-9]+)")
        Dim match As Match = regex.Match(input)
        If match.Success Then
            Return Tuple.Create(row_letter_to_number(match.Groups(1).Value), CInt(match.Groups(2).Value))
        Else
            Return Tuple.Create(0, 0)
        End If
    End Function

    Public Function generate_plate_map_square(
        START_WELL As String,
        END_WELL As String,
        sample_name As String,
        plate_size As Integer
        ) As String(,)
        Dim plate_array(plate_size, 3) As String
        Dim rlen As Integer
        Dim clen As Integer
        Select Case plate_size
            Case 24
                rlen = 6
                clen = 4
            Case 96
                rlen = 12
                clen = 8
            Case 384
                rlen = 48
                clen = 16
            Case Else
                Throw New ArgumentException("plate_size must be 24, 96, or 384")
        End Select
        Dim tl_rc As Tuple(Of Integer, Integer) = string_row_column(START_WELL)
        Dim tl_row = tl_rc.Item1
        Dim tl_col = tl_rc.Item2
        Dim br_rc As Tuple(Of Integer, Integer) = string_row_column(END_WELL)
        Dim br_row = br_rc.Item1
        Dim br_col = br_rc.Item2

        Dim current_col As Integer
        Dim current_row As Integer
        Dim sample_idx As Integer = 1
        For i As Integer = 1 To plate_size
            plate_array(i, 0) = i.ToString()
            current_row = (i - 1) Mod clen + 1
            current_col = (i - 1) \ clen + 1
            If current_row >= tl_row AndAlso current_row <= br_row AndAlso
                current_col >= tl_col AndAlso current_col <= br_col Then
                plate_array(i, 1) = "S"
                plate_array(i, 2) = String.Format("{0} {1}", sample_name, sample_idx)
                plate_array(i, 3) = "55bb55"
                sample_idx += 1
            Else
                plate_array(i, 1) = "-"
                plate_array(i, 2) = "-"
                plate_array(i, 3) = "555555"
            End If
        Next
        Return plate_array
    End Function



    Public Function generate_plate_map_linear(
        N_SAMPLES As Integer,
        OFFSET_SAMPLES As Integer,
        sample_name As String,
        plate_size As Integer
        ) As String(,)
        Dim plate_array(plate_size, 3) As String
        'VB actually created an array of plate_size + 1 elements, 4 slots
        'This script will not use index 0
        For i As Integer = 1 To plate_size
            plate_array(i, 0) = i.ToString()
            If OFFSET_SAMPLES < i AndAlso i <= (OFFSET_SAMPLES + N_SAMPLES) Then
                plate_array(i, 1) = "S"
                plate_array(i, 2) = String.Format("{0} {1}", sample_name, i)
                plate_array(i, 3) = "55bb55"
            Else
                plate_array(i, 1) = "-"
                plate_array(i, 2) = "-"
                plate_array(i, 3) = "555555"
            End If
        Next
        Return plate_array
    End Function

    Public Function change_plate_map(
        ByRef plate_array As String(,),
        idx As Integer,
        Optional marker As String = Nothing,
        Optional note As String = Nothing,
        Optional color As String = Nothing
        ) As Boolean
        ' Check if idx is within the bounds of plate_array
        If idx < 0 Or idx >= plate_array.GetLength(0) Then
            Return False
        End If
        If marker IsNot Nothing Then
            plate_array(idx, 1) = marker
        End If
        If note IsNot Nothing Then
            plate_array(idx, 2) = note
        End If
        If color IsNot Nothing Then
            plate_array(idx, 3) = color
        End If
        Return True
    End Function

    Public Function write_plate_map(
        plate_array As String(,),
        plate_map_file_path As String
        ) As Boolean
        Using writer As New StreamWriter(plate_map_file_path)
            For i As Integer = 1 To plate_array.GetLength(0) - 1
                writer.WriteLine(
                String.Format("{0};{1};{2};{3}",
                plate_array(i, 0),
                plate_array(i, 1),
                plate_array(i, 2),
                plate_array(i, 3))
                )
            Next
        End Using
        Return True
    End Function

    Public Sub Execute() Implements IScriptObject.Execute

        Dim N_SAMPLES As Integer = Host.GetVariable("N_SAMPLES")
        Dim N_10xDIL As Integer = Host.GetVariable("N_10xDIL")
        Dim currentDateAndTime As String = DateTime.Now.ToString("yyyyMMdd_HHmm")
        Dim source_map_file As String = String.Format(
        "C:\Users\Public\Documents\CFU_dilution_plating\source_map_24_tubes_{0}.txt",
        currentDateAndTime
        )
        Host.SetVariable("source_map_file", source_map_file)

        ' Generate plate map
        'Dim plate_array As String(,) = generate_plate_map_linear(N_SAMPLES, 0, "Stock", 24)
        Dim plate_array As String(,) = generate_plate_map_square("a2", "b6", "Stock", 24)
        write_plate_map(plate_array, source_map_file)

        Dim n_dilution_plates As Integer
        Dim n_target_plates As Integer
        If N_10xDIL <= 6 Then
            'Dilution plate can host 16 samples
            n_dilution_plates = CInt(Math.Ceiling(N_SAMPLES / 16))
        Else
            'Dilution plate can host 8 samples
            n_dilution_plates = CInt(Math.Ceiling(N_SAMPLES / 8))
        End If
        Host.SetVariable("n_dilution_plates", n_dilution_plates)




    End Sub

End Class