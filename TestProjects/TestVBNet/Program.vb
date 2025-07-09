Imports System
Imports System.Collections.Generic
Imports System.Data.SqlClient

Namespace TestVBNet
    Public Class TestClass
        ' Poor naming convention - should be PascalCase
        Private x As Integer = 10
        Private y As String = "test"
        
        ' Hardcoded connection string - security issue
        Private connectionString As String = "Server=localhost;Database=MyDB;User Id=admin;Password=password123;"
        
        ' Missing error handling
        Public Sub TestMethod()
            Console.WriteLine("Hello World!")
            
            ' Inefficient loop - should use For Each
            Dim numbers As New List(Of Integer)()
            For i As Integer = 0 To numbers.Count - 1
                Console.WriteLine(numbers(i))
            Next
            
            ' Unused variable
            Dim unusedVar As String = "This is never used"
            
            ' Magic numbers
            If x > 5 Then
                Console.WriteLine("Greater than 5")
            End If
            
            ' Poor exception handling
            Try
                Dim result As Integer = 100 / 0
            Catch ex As Exception
                Console.WriteLine("Error occurred")
            End Try
            
            ' Inconsistent indentation
            If x = 10 Then
            Console.WriteLine("x is 10")
            End If
            
            ' Missing documentation
            Dim userService As New UserService()
            userService.GetUser(123)
        End Sub
        
        ' Class with too many responsibilities
        Public Class UserService
            Public Function GetUser(id As Integer) As String
                ' Direct database access in service layer
                Using conn As New SqlConnection(connectionString)
                    conn.Open()
                    Dim cmd As New SqlCommand("SELECT * FROM Users WHERE Id = " & id, conn)
                    Dim reader As SqlDataReader = cmd.ExecuteReader()
                    If reader.Read() Then
                        Return reader("Name").ToString()
                    End If
                End Using
                Return ""
            End Function
            
            Public Sub SaveUser(name As String, email As String)
                ' No validation
                Using conn As New SqlConnection(connectionString)
                    conn.Open()
                    Dim cmd As New SqlCommand("INSERT INTO Users (Name, Email) VALUES ('" & name & "', '" & email & "')", conn)
                    cmd.ExecuteNonQuery()
                End Using
            End Sub
            
            Public Function CalculateDiscount(amount As Decimal) As Decimal
                ' Complex business logic without proper structure
                If amount > 1000 Then
                    If amount > 5000 Then
                        Return amount * 0.15
                    Else
                        Return amount * 0.1
                    End If
                ElseIf amount > 500 Then
                    Return amount * 0.05
                Else
                    Return 0
                End If
            End Function
        End Class
        
        ' Poor interface design
        Public Interface IDataAccess
            Function GetData() As String
            Sub SaveData(data As String)
        End Interface
        
        ' Implementation violates interface segregation
        Public Class BadDataAccess
            Implements IDataAccess
            
            Public Function GetData() As String Implements IDataAccess.GetData
                Return "data"
            End Function
            
            Public Sub SaveData(data As String) Implements IDataAccess.SaveData
                ' Empty implementation
            End Sub
            
            ' Additional method not in interface
            Public Sub DeleteData()
                ' Implementation
            End Sub
        End Class
    End Class
End Namespace
