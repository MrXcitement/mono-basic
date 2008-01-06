' 
' Visual Basic.Net Compiler
' Copyright (C) 2004 - 2007 Rolf Bjarne Kvinge, RKvinge@novell.com
' 
' This library is free software; you can redistribute it and/or
' modify it under the terms of the GNU Lesser General Public
' License as published by the Free Software Foundation; either
' version 2.1 of the License, or (at your option) any later version.
' 
' This library is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY; without even the implied warranty of
' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
' Lesser General Public License for more details.
' 
' You should have received a copy of the GNU Lesser General Public
' License along with this library; if not, write to the Free Software
' Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
' 

#If DEBUG Then
#Const EXTENDEDDEBUG = 0
#End If

Public MustInherit Class MethodDeclaration
    Inherits MethodBaseDeclaration

    Private m_Descriptor As New MethodDescriptor(Me)
    Private m_MethodBuilder As MethodBuilder

#If ENABLECECIL Then
    Private m_CecilBuilder As Mono.Cecil.MethodDefinition
#End If

    Protected Sub New(ByVal Parent As TypeDeclaration)
        MyBase.new(Parent)
    End Sub

    Protected Sub New(ByVal Parent As PropertyDeclaration)
        MyBase.new(Parent)
    End Sub

    Protected Sub New(ByVal Parent As EventDeclaration)
        MyBase.new(Parent)
    End Sub

    ReadOnly Property Descriptor() As MethodDescriptor
        Get
            Return m_Descriptor
        End Get
    End Property

    Public Overrides ReadOnly Property MemberDescriptor() As System.Reflection.MemberInfo
        Get
            Return m_Descriptor
        End Get
    End Property

    Public Overrides ReadOnly Property MethodBuilder() As System.Reflection.Emit.MethodBuilder
        Get
            Return m_MethodBuilder
        End Get
    End Property

#If ENABLECECIL Then
    Public Overrides ReadOnly Property CecilBuilder() As Mono.Cecil.MethodDefinition
        Get
            Return m_CecilBuilder
        End Get
    End Property
#End If

    Public Overrides ReadOnly Property MethodDescriptor() As System.Reflection.MethodBase
        Get
            Return m_Descriptor
        End Get
    End Property

    Public Overrides Function ResolveTypeReferences() As Boolean
        Dim result As Boolean = True

        result = MyBase.ResolveTypeReferences AndAlso result
        'vbnc.Helper.Assert(result = (Report.Errors = 0))

        Return result
    End Function

    Shadows Sub Init(ByVal Attributes As Attributes, ByVal Modifiers As Modifiers, ByVal Signature As SubSignature)
        MyBase.Init(Attributes, Modifiers, Signature)

#If ENABLECECIL Then
        m_CecilBuilder = New Mono.Cecil.MethodDefinition(Name, 0, Compiler.CecilTypeCache.System_Void)
#End If
    End Sub

    Shadows Sub Init(ByVal Attributes As Attributes, ByVal Modifiers As Modifiers, ByVal Signature As SubSignature, ByVal Code As CodeBlock)
        MyBase.Init(Attributes, Modifiers, Signature, Code)
#If ENABLECECIL Then
        m_CecilBuilder = New Mono.Cecil.MethodDefinition(Name, 0, Compiler.CecilTypeCache.System_Void)
#End If
    End Sub

    Public Overrides Function ResolveMember(ByVal Info As ResolveInfo) As Boolean
        Dim result As Boolean = True

        result = MyBase.ResolveMember(Info) AndAlso result

#If ENABLECECIL Then
        DeclaringType.CecilType.Methods.Add(m_CecilBuilder)
        m_CecilBuilder.HasThis = Not IsShared
#End If

        Return result
    End Function

    Public Overrides Function ResolveCode(ByVal Info As ResolveInfo) As Boolean
        Dim result As Boolean = True

        result = MyBase.ResolveCode(Info) AndAlso result


        Return result
    End Function

    Public Overrides Function DefineMember() As Boolean
        Dim result As Boolean = True

        'Helper.StopIfDebugging(Me.Name = "GetSpecificMembers")

        result = MyBase.DefineMember AndAlso result

        Helper.Assert(ParameterTypes IsNot Nothing)
        Helper.Assert(Me.DeclaringType IsNot Nothing)
        Helper.Assert(Me.DeclaringType.TypeBuilder IsNot Nothing)
        Helper.Assert(m_MethodBuilder Is Nothing)

#If EXTENDEDDEBUG Then
        If Me.DeclaringType IsNot Nothing Then
            Compiler.Report.WriteLine("$Defining method " & Name & " on type=" & Me.DeclaringType.FullName & " with attributes=" & Attributes.ToString & ", = " & CInt(Attributes))
        Else
            Compiler.Report.WriteLine("$Defining method " & Name & " with attributes=" & Attributes.ToString & ", = " & CInt(Attributes))
        End If
#End If
        m_MethodBuilder = DeclaringType.TypeBuilder.DefineMethod(Name, Attributes)

#If ENABLECECIL Then
        m_CecilBuilder.Attributes = CType(Attributes, Mono.Cecil.MethodAttributes)
#End If

#If DEBUGREFLECTION Then
        Helper.DebugReflection_AppendLine("{0} = {1}.DefineMethod(""{2}"", CType({3}, System.Reflection.MethodAttributes))", m_MethodBuilder, DeclaringType.TypeBuilder, Name, CInt(Attributes).ToString())
#End If
        Compiler.TypeManager.RegisterReflectionMember(m_MethodBuilder, Me.MemberDescriptor)

        If Signature.TypeParameters IsNot Nothing Then
            result = Signature.TypeParameters.Parameters.DefineGenericParameters(m_MethodBuilder) AndAlso result
        End If

        If ReturnType IsNot Nothing Then
            ReturnType = Helper.GetTypeOrTypeBuilder(ReturnType)
        End If
#If EXTENDEDDEBUG Then
            If ReturnType Is Nothing Then
                Compiler.Report.WriteLine("$>Setting return type to nothing")
            Else
                Compiler.Report.WriteLine("$>Setting return type to:" & ReturnType.FullName)
            End If
#End If
        m_MethodBuilder.SetReturnType(ReturnType)
#If ENABLECECIL Then
        If ReturnType Is Nothing Then
            m_CecilBuilder.ReturnType.ReturnType = Helper.GetTypeOrTypeReference(Compiler, Compiler.CecilTypeCache.System_Void)
        Else
            m_CecilBuilder.ReturnType.ReturnType = Helper.GetTypeOrTypeReference(Compiler, ReturnType)
        End If
#End If
        Helper.SetTypeOrTypeBuilder(ParameterTypes)
        m_MethodBuilder.SetParameters(ParameterTypes)
        If Signature.Parameters IsNot Nothing Then
            For i As Integer = 0 To Signature.Parameters.Count - 1
                Dim param As Parameter
                param = Signature.Parameters(i)
                result = Signature.Parameters(i).Define(m_MethodBuilder) AndAlso result
#If ENABLECECIL Then
                result = Signature.Parameters(i).Define(m_CecilBuilder) AndAlso result
#End If
            Next
        End If

        If MethodImplAttributes.HasValue Then
#If EXTENDEDDEBUG Then
                Compiler.Report.WriteLine("$>Setting impl attributes= " & MethodImplAttributes.ToString)
#End If
            m_MethodBuilder.SetImplementationFlags(MethodImplAttributes.Value)
#If ENABLECECIL Then
            m_CecilBuilder.ImplAttributes = CType(MethodImplAttributes.Value, Mono.Cecil.MethodImplAttributes)
#End If
        End If

        '        Compiler.Helper.DumpDefine(Compiler, m_MethodBuilder)

        Return result
    End Function

    Friend Overrides Function GenerateCode(ByVal Info As EmitInfo) As Boolean
        Dim result As Boolean = True

        result = MyBase.GenerateCode(Info) AndAlso result

        If Signature.Parameters IsNot Nothing Then
            For i As Integer = 0 To Signature.Parameters.Count - 1
                result = Signature.Parameters(i).GenerateCode(Info) AndAlso result
            Next
        End If
        Return result
    End Function

    Public Overrides ReadOnly Property ILGenerator() As System.Reflection.Emit.ILGenerator
        Get
            Return m_MethodBuilder.GetILGenerator
        End Get
    End Property
End Class