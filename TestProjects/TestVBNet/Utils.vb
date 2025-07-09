Imports System
Imports System.Collections.Generic
Imports System.Linq

Namespace TestVBNet
    ''' <summary>
    ''' Utility module for testing VB.NET prompt functionality
    ''' </summary>
    Public Module Utils
        ''' <summary>
        ''' Calculates the sum of numbers in a list
        ''' </summary>
        ''' <param name="numbers">List of numbers to sum</param>
        ''' <returns>The sum of all numbers</returns>
        Public Function CalculateSum(numbers As List(Of Integer)) As Integer
            If numbers Is Nothing Then
                Throw New ArgumentNullException(NameOf(numbers))
            End If
            
            Return numbers.Sum()
        End Function

        ''' <summary>
        ''' Filters a list based on a predicate
        ''' </summary>
        ''' <typeparam name="T">Type of items in the list</typeparam>
        ''' <param name="items">List to filter</param>
        ''' <param name="predicate">Filter condition</param>
        ''' <returns>Filtered list</returns>
        Public Function Filter(Of T)(items As List(Of T), predicate As Func(Of T, Boolean)) As List(Of T)
            If items Is Nothing Then
                Throw New ArgumentNullException(NameOf(items))
            End If
            
            If predicate Is Nothing Then
                Throw New ArgumentNullException(NameOf(predicate))
            End If
            
            Return items.Where(Function(item) predicate(item)).ToList()
        End Function

        ''' <summary>
        ''' Validates an email address format
        ''' </summary>
        ''' <param name="email">Email to validate</param>
        ''' <returns>True if valid email format</returns>
        Public Function IsValidEmail(email As String) As Boolean
            If String.IsNullOrWhiteSpace(email) Then
                Return False
            End If
            
            Try
                Dim addr As New System.Net.Mail.MailAddress(email)
                Return addr.Address = email
            Catch
                Return False
            End Try
        End Function
    End Module
End Namespace 