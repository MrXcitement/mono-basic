Module ExplicitConversionByteToShort1
    Function Main() As Integer
        Dim result As Boolean
        Dim value1 As Byte
        Dim value2 As Short
        Dim const2 As Short

        value1 = CByte(10)
        value2 = CShort(value1)
        const2 = CShort(CByte(10))

        result = value2 = const2

        If result = False Then
            System.Console.WriteLine("FAIL ExplicitConversionByteToShort1")
            Return 1
        End If
    End Function
End Module
