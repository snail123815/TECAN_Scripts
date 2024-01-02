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
        ' Specify the path to your file
        Dim plateMapFilePath As String = "C:\Users\Public\Documents\proteomics\STAGETipping_addC_plateMap.txt"

        Dim N_SAMPLES As Integer = Host.GetVariable("N_SAMPLES")
        Dim OFFSET_SAMPLES As Integer = Host.GetVariable("OFFSET_SAMPLES")
        Dim MAKE_MIX As Boolean = Host.GetVariable("MAKE_MIX")

        ' Calculate mix well location
        Dim mixWell As Integer = 0
        If MAKE_MIX Then
            If OFFSET_SAMPLES + N_SAMPLES >= 96 Then
                mixWell = 0
            ElseIf OFFSET_SAMPLES + N_SAMPLES = 95 Then
                mixWell = 96
            Else
                mixWell = OFFSET_SAMPLES + N_SAMPLES + 2
            End If
        End If
        Host.SetVariable("mixWell", mixWell)

        ' Make result array
        Dim resultArray(96, 3) As String
        For index As Integer = 1 To 96
            resultArray(index, 0) = index.ToString()
            If OFFSET_SAMPLES < index AndAlso index <= (OFFSET_SAMPLES + N_SAMPLES) Then
                resultArray(index, 1) = "S"
                resultArray(index, 2) = String.Format("Sample No. {0}", index-OFFSET_SAMPLES)
                resultArray(index, 3) = "55bb55"
            ElseIf index = mixWell
                resultArray(index, 1) = "M"
                resultArray(index, 2) = "Mix of all"
                resultArray(index, 3) = "5555dd"
            Else
                resultArray(index, 1) = "-"
                resultArray(index, 2) = "-"
                resultArray(index, 3) = "555555"
            End If
        Next

        
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