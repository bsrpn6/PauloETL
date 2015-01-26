Option Strict Off
Option Explicit On
Imports Microsoft.VisualBasic
Imports System
Imports System.Collections
Imports System.Collections.Specialized
Imports System.Reflection
Imports System.Xml
'UPGRADE_NOTE: (1043) Class instancing was changed to public. More Information: http://www.vbtonet.com/ewis/ewi1043.aspx
<DefaultMember("Item")> _
Public Class ETLConnections
	Implements IEnumerable
	'Constant for Module Name Used In Error Functions
	Const cModule As String = "ETLConnections."

	'local variable to hold collection
	Private mCol As OrderedDictionary
	Private moContextNode As XmlNode

	Friend Function LoadFromXML(ByRef XMLDoc As XmlDocument, ByRef ErrorMessage As String) As Integer
		Const cProcedure As String = "LoadFromXML()"
		Dim lReturn, lNodeCount As Integer
		Try
            'cProcedure = "LoadFromXML()"
			Dim oNodeList As XmlNodeList
			Dim oNode As XmlElement
			Dim oETLConnection As ETLConnection
            'lReturn = "lNodeCount"

			lReturn = 0
			'Load NodeList of Connections
			oNodeList = XMLDoc.SelectNodes("//root/connections/connection")
			lNodeCount = oNodeList.Count
			If lNodeCount < 2 Then
				ErrorMessage = MainErrHandler(0, "Error, File Must Have At Least 2 Connections Defined", cModule & cProcedure)
			Else
				'Loop Through Connections
                'For I As Integer = 1 To lNodeCount
                '	'UPGRADE_WARNING: (2065) MSXML2.IXMLDOMNodeList method oNodeList.nextNode has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2065.aspx
                '                oNode = oNodeList.GetEnumerator().Current
                '	oETLConnection = New ETLConnection()
                '	If oETLConnection.LoadFromXML(oNode, ErrorMessage) Then
                '		mCol.Add(oETLConnection.ID, oETLConnection)
                '	Else
                '		lNodeCount = 0
                '		Exit For
                '	End If
                '            Next I

                'ADAM CODE
                Dim oNodeEnumerator 'As System.Xml.XmlNodeListEnumerator
                oNodeEnumerator = oNodeList.GetEnumerator()

                While oNodeEnumerator.MoveNext()
                    oNode = oNodeEnumerator.Current
                    oETLConnection = New ETLConnection()
                    If oETLConnection.LoadFromXML(oNode, ErrorMessage) Then
                        mCol.Add(oETLConnection.ID, oETLConnection)
                    Else
                        lNodeCount = 0
                        Exit While
                    End If
                End While


				lReturn = lNodeCount

			End If

		Catch excep As System.Exception

			ErrorMessage = MainErrHandler(Information.Err().Number, excep.Message, cModule & cProcedure)

		Finally 
			MainDebug("Function Exit: " & lReturn, cModule & cProcedure)
		End Try
		Return lReturn

	End Function

	Default Public ReadOnly Property Item(ByVal vntIndexKey As Object) As ETLConnection
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