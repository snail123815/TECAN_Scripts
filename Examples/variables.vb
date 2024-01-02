Imports Microsoft.VisualBasic
Imports System
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
        ' INSERT YOUR CODE HERE
        Host.ResolveExpression("array[2]:=5")
        Host.ResolveExpression("array[3]:=0")
        ' Get ubound of an array
        Dim countsOfArray as Integer = Host.ResolveExpression("count(array[])")
        Host.SetVariable("countsOfArray", countsOfArray)
        ' Will be 2 although array[3] is the same as default

        ' Get a value from an array
        Dim textItem as String = Host.ResolveExpression("arrayStr[4]")
        ' Set a text value in the array
        Host.ResolveExpression("arrayStr[5]:=""cat""")
        ' Host.ResolveExpression(String.Format("arrayStr[6]:=""{0}""",textItem))
        Host.ResolveExpression(("arrayStr[6]:="""+textItem+""""))

        Dim N_SAMPLES As Integer = Host.GetVariable("N_SAMPLES")

    End Sub

End Class
