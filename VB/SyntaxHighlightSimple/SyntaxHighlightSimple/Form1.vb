Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Drawing
Imports System.Windows.Forms
Imports DevExpress.CodeParser
Imports DevExpress.CodeParser.CSharp
Imports DevExpress.LookAndFeel
Imports DevExpress.Skins
Imports DevExpress.XtraRichEdit
Imports DevExpress.XtraRichEdit.API.Native
Imports DevExpress.XtraRichEdit.Services

Namespace SyntaxHighlightSimple

	Partial Public Class Form1
		Inherits Form
		Public Sub New()
			Dim path As String = "Form1.cs"
			InitializeComponent()

			' Use service substitution to register a custom service that implements highlighting.
			Dim service As ISyntaxHighlightService = richEditControl1.GetService(Of ISyntaxHighlightService)()
			Dim svc As New MySyntaxHighlightService(richEditControl1)
			richEditControl1.RemoveService(GetType(ISyntaxHighlightService))
			richEditControl1.AddService(GetType(ISyntaxHighlightService), svc)

			richEditControl1.LoadDocument(path)
		End Sub
	End Class
	''' <summary>
	'''  This class implements the Execute method of the ISyntaxHighlightService interface to parse and colorize the text.
	''' </summary>
	Public Class MySyntaxHighlightService
		Implements ISyntaxHighlightService
		Private ReadOnly syntaxEditor As RichEditControl
		Private syntaxColors As SyntaxColors
		Private commentProperties As SyntaxHighlightProperties
		Private keywordProperties As SyntaxHighlightProperties
		Private stringProperties As SyntaxHighlightProperties
		Private xmlCommentProperties As SyntaxHighlightProperties
		Private textProperties As SyntaxHighlightProperties

		Public Sub New(ByVal syntaxEditor As RichEditControl)
			Me.syntaxEditor = syntaxEditor
			syntaxColors = New SyntaxColors(UserLookAndFeel.Default)
		End Sub

		Private Sub HighlightSyntax(ByVal tokens As TokenCollection)
			commentProperties = New SyntaxHighlightProperties()
			commentProperties.ForeColor = syntaxColors.CommentColor

			keywordProperties = New SyntaxHighlightProperties()
			keywordProperties.ForeColor = syntaxColors.KeywordColor

			stringProperties = New SyntaxHighlightProperties()
			stringProperties.ForeColor = syntaxColors.StringColor

			xmlCommentProperties = New SyntaxHighlightProperties()
			xmlCommentProperties.ForeColor = syntaxColors.XmlCommentColor

			textProperties = New SyntaxHighlightProperties()
			textProperties.ForeColor = syntaxColors.TextColor

			If tokens Is Nothing OrElse tokens.Count = 0 Then
				Return
			End If

			Dim document As Document = syntaxEditor.Document
			'CharacterProperties cp = document.BeginUpdateCharacters(0, 1);
			Dim syntaxTokens As New List(Of SyntaxHighlightToken)(tokens.Count)
			For Each token As Token In tokens
				HighlightCategorizedToken(CType(token, CategorizedToken), syntaxTokens)
			Next token
			document.ApplySyntaxHighlight(syntaxTokens)
			'document.EndUpdateCharacters(cp);
		End Sub
		Private Sub HighlightCategorizedToken(ByVal token As CategorizedToken, ByVal syntaxTokens As List(Of SyntaxHighlightToken))
			Dim backColor As Color = syntaxEditor.ActiveView.BackColor
			Dim category As TokenCategory = token.Category
			If category = TokenCategory.Comment Then
				syntaxTokens.Add(SetTokenColor(token, commentProperties, backColor))
			ElseIf category = TokenCategory.Keyword Then
				syntaxTokens.Add(SetTokenColor(token, keywordProperties, backColor))
			ElseIf category = TokenCategory.String Then
				syntaxTokens.Add(SetTokenColor(token, stringProperties, backColor))
			ElseIf category = TokenCategory.XmlComment Then
				syntaxTokens.Add(SetTokenColor(token, xmlCommentProperties, backColor))
			Else
				syntaxTokens.Add(SetTokenColor(token, textProperties, backColor))
			End If
		End Sub
		Private Function SetTokenColor(ByVal token As Token, ByVal foreColor As SyntaxHighlightProperties, ByVal backColor As Color) As SyntaxHighlightToken
			If syntaxEditor.Document.Paragraphs.Count < token.Range.Start.Line Then
				Return Nothing
			End If
			Dim paragraphStart As Integer = DocumentHelper.GetParagraphStart(syntaxEditor.Document.Paragraphs(token.Range.Start.Line - 1))
			Dim tokenStart As Integer = paragraphStart + token.Range.Start.Offset - 1
			If token.Range.End.Line <> token.Range.Start.Line Then
				paragraphStart = DocumentHelper.GetParagraphStart(syntaxEditor.Document.Paragraphs(token.Range.End.Line - 1))
			End If

			Dim tokenEnd As Integer = paragraphStart + token.Range.End.Offset - 1
			Debug.Assert(tokenEnd > tokenStart)
			Return New SyntaxHighlightToken(tokenStart, tokenEnd - tokenStart, foreColor)
		End Function

		#Region "#ISyntaxHighlightServiceMembers"
		Public Sub Execute() Implements ISyntaxHighlightService.Execute
			Dim newText As String = syntaxEditor.Text
			' Use DevExpress.CodeParser to parse text into tokens.
			Dim highlightTokens As TokenCollection
			highlightTokens = CSharpTokensHelper.GetTokens(newText)
			HighlightSyntax(highlightTokens)
		End Sub

		Public Sub ForceExecute() Implements ISyntaxHighlightService.ForceExecute
			Execute()
		End Sub
		#End Region '#ISyntaxHighlightServiceMembers
	End Class
	''' <summary>
	'''  This class provides colors to highlight the tokens.
	''' </summary>
	Public Class SyntaxColors
		Private Shared ReadOnly Property DefaultCommentColor() As Color
			Get
				Return Color.Green
			End Get
		End Property
		Private Shared ReadOnly Property DefaultKeywordColor() As Color
			Get
				Return Color.Blue
			End Get
		End Property
		Private Shared ReadOnly Property DefaultStringColor() As Color
			Get
				Return Color.Brown
			End Get
		End Property
		Private Shared ReadOnly Property DefaultXmlCommentColor() As Color
			Get
				Return Color.Gray
			End Get
		End Property
		Private Shared ReadOnly Property DefaultTextColor() As Color
			Get
				Return Color.Black
			End Get
		End Property
		Private lookAndFeel As UserLookAndFeel

		Public ReadOnly Property CommentColor() As Color
			Get
				Return GetCommonColorByName(CommonSkins.SkinInformationColor, DefaultCommentColor)
			End Get
		End Property
		Public ReadOnly Property KeywordColor() As Color
			Get
				Return GetCommonColorByName(CommonSkins.SkinQuestionColor, DefaultKeywordColor)
			End Get
		End Property
		Public ReadOnly Property TextColor() As Color
			Get
				Return GetCommonColorByName(CommonColors.WindowText, DefaultTextColor)
			End Get
		End Property
		Public ReadOnly Property XmlCommentColor() As Color
			Get
				Return GetCommonColorByName(CommonColors.DisabledText, DefaultXmlCommentColor)
			End Get
		End Property
		Public ReadOnly Property StringColor() As Color
			Get
				Return GetCommonColorByName(CommonSkins.SkinWarningColor, DefaultStringColor)
			End Get
		End Property

		Public Sub New(ByVal lookAndFeel As UserLookAndFeel)
			Me.lookAndFeel = lookAndFeel
		End Sub

		Private Function GetCommonColorByName(ByVal colorName As String, ByVal defaultColor As Color) As Color
			Dim skin As Skin = CommonSkins.GetSkin(lookAndFeel)
			If skin Is Nothing Then
				Return defaultColor
			End If
			Return skin.Colors(colorName)
		End Function
	End Class
End Namespace

