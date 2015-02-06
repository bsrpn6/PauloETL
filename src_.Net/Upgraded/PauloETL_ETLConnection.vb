Option Strict Off
Option Explicit On
Imports Microsoft.VisualBasic
Imports System
Imports System.Data
Imports System.Data.Common
Imports System.Xml
Imports UpgradeHelpers.Helpers

Public Class ETLConnection
	'Constant for Module Name Used In Error Functions
	Const cModule As String = "ETLControl."

	Private moCn As DbConnection
	Private msID As String = ""
	Private msName As String = ""
	Private msUID As String = ""
	Private msPWD As String = ""

	Friend Function LoadFromXML(ByRef oNode As XmlElement, ByRef ErrorMessage As String) As Boolean
		Const cProcedure As String = "LoadFromXML()"
		Dim bReturn As Boolean = False
		Try

			bReturn = False
			msID = ReflectionHelper.GetPrimitiveValue(Of String)(oNode.GetAttribute("id"))
			msName = ReflectionHelper.GetPrimitiveValue(Of String)(oNode.GetAttribute("name"))
			moCn.ConnectionString = ReflectionHelper.GetPrimitiveValue(Of String)(oNode.GetAttribute("connstring"))
			msUID = ReflectionHelper.GetPrimitiveValue(Of String)(oNode.GetAttribute("uid"))
			msPWD = ReflectionHelper.GetPrimitiveValue(Of String)(oNode.GetAttribute("pwd"))
			bReturn = True

		Catch excep As System.Exception

			ErrorMessage = MainErrHandler(Information.Err().Number, excep.Message, cModule & cProcedure, "Connection: " & msID)

		Finally 
			MainDebug("Function Exit: " & bReturn, cModule & cProcedure)
		End Try

		Return bReturn

	End Function

	Public Function OpenConnection(ByRef ErrorMessage As String) As Boolean
		Const cProcedure As String = "OpenConnection()"
		Dim bReturn As Boolean = False
		Try

			bReturn = False
            'UNCOMMENT - CONNECTION
            moCn.Open()
			bReturn = True

		Catch excep As System.Exception

			ErrorMessage = MainErrHandler(Information.Err().Number, excep.Message, cModule & cProcedure, "Connection: " & msID)

		Finally 
			MainDebug("Function Exit: " & bReturn, cModule & cProcedure)
		End Try

		Return bReturn

	End Function

	Public ReadOnly Property IsOpen() As Boolean
		Get
			Return moCn.State = ConnectionState.Open
		End Get
	End Property

	Public ReadOnly Property Cn() As DbConnection
		Get
			Return moCn
		End Get
	End Property


	Public Property ID() As String
		Get
			Return msID
		End Get
		Set(ByVal Value As String)
			msID = Value
		End Set
	End Property


	Public Property Name() As String
		Get
			Return msName
		End Get
		Set(ByVal Value As String)
			msName = Value
		End Set
	End Property

	Friend Sub New()
		MyBase.New()
		moCn = UpgradeHelpers.DB.AdoFactoryManager.GetFactory().CreateConnection()
	End Sub

	Protected Overrides Sub Finalize()
		moCn = Nothing
	End Sub
End Class