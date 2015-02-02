Option Strict Off
Option Explicit On
Imports Microsoft.VisualBasic
Imports System
Imports System.Collections
Imports System.Collections.Specialized
Imports System.Reflection
Imports System.Xml

<DefaultMember("Item")> _
Public Class ETLCommands
	Implements IEnumerable
	'Constant for Module Name Used In Error Functions
	Const cModule As String = "ETLCommands."

	'local variable to hold collection
	Private mCol As OrderedDictionary
	Private moContextNode As XmlNode

	Friend Function LoadFromXML(ByRef ContextNode As XmlElement, ByRef oETLControl As ETLControl, ByVal ParentLocation As String, ByRef ErrorMessage As String, Optional ByRef ParentCommand As ETLCommand = Nothing) As Integer
		Const cProcedure As String = "LoadFromXML()"
		Dim lReturn, lNodeCount As Integer
		Try

			Dim oNodeList As XmlNodeList
			Dim oNode As XmlElement
			Dim oETLCommand As ETLCommand

			lReturn = -1
			'Load NodeList of Connections
			oNodeList = ContextNode.SelectNodes("command")
			lNodeCount = oNodeList.Count
			If lNodeCount = 0 Then
				lReturn = 0
			Else
				'Loop Through Connections
				For I As Integer = 1 To lNodeCount

                    oNode = oNodeList.Item(I - 1)
					oETLCommand = New ETLCommand()
					If oETLCommand.LoadFromXML(oNode, oETLControl, ParentLocation, ErrorMessage, ParentCommand) Then
						mCol.Add(Guid.NewGuid().ToString(), oETLCommand)
					Else
						lNodeCount = -1
						Exit For
					End If
				Next I
				lReturn = lNodeCount

			End If

		Catch excep As System.Exception

			ErrorMessage = MainErrHandler(Information.Err().Number, excep.Message, cModule & cProcedure)

		Finally 
			MainDebug("Function Exit: " & lReturn, cModule & cProcedure)
		End Try
		Return lReturn

	End Function

	Default Public ReadOnly Property Item(ByVal vntIndexKey As Object) As ETLCommand
		Get
			'used when referencing an element in the collection
			'vntIndexKey contains either the Index or Key to the collection,
			'this is why it is declared as a Variant
			'Syntax: Set foo = x.Item(xyz) or Set foo = x.Item(5)
			Return mCol(vntIndexKey)
		End Get
	End Property

	Public ReadOnly Property Count() As Integer
		Get
			'used when retrieving the number of elements in the
			'collection. Syntax: Debug.Print x.Count
			Return mCol.Count
		End Get
	End Property

	'UPGRADE_ISSUE: (2068) IUnknown object was not upgraded. More Information: http://www.vbtonet.com/ewis/ewi2068.aspx

	Public Function GetEnumerator() As IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
		'this property allows you to enumerate
        'this collection with the For...Each syntax

        Return mCol.GetEnumerator()

    End Function

	Friend Sub New()
		MyBase.New()
		'creates the collection when this class is created
		mCol = New OrderedDictionary()
	End Sub

	Protected Overrides Sub Finalize()
		'destroys collection when this class is terminated
		mCol = Nothing
	End Sub
End Class