
#define ICALL_TABLE_corlib 1

static int corlib_icall_indexes [] = {
    /* 0 */ 220,
    /* 1 */ 231,
    /* 2 */ 232,
    /* 3 */ 233,
    /* 4 */ 234,
    /* 5 */ 235,
    /* 6 */ 236,
    /* 7 */ 237,
    /* 8 */ 238,
    /* 9 */ 239,
    /* 10 */ 242,
    /* 11 */ 243,
    /* 12 */ 370,
    /* 13 */ 371,
    /* 14 */ 372,
    /* 15 */ 400,
    /* 16 */ 401,
    /* 17 */ 402,
    /* 18 */ 429,
    /* 19 */ 430,
    /* 20 */ 431,
    /* 21 */ 531,
    /* 22 */ 534,
    /* 23 */ 574,
    /* 24 */ 575,
    /* 25 */ 576,
    /* 26 */ 579,
    /* 27 */ 581,
    /* 28 */ 585,
    /* 29 */ 587,
    /* 30 */ 592,
    /* 31 */ 600,
    /* 32 */ 601,
    /* 33 */ 602,
    /* 34 */ 603,
    /* 35 */ 604,
    /* 36 */ 605,
    /* 37 */ 606,
    /* 38 */ 607,
    /* 39 */ 608,
    /* 40 */ 661,
    /* 41 */ 662,
    /* 42 */ 663,
    /* 43 */ 664,
    /* 44 */ 665,
    /* 45 */ 666,
    /* 46 */ 667,
    /* 47 */ 668,
    /* 48 */ 669,
    /* 49 */ 670,
    /* 50 */ 671,
    /* 51 */ 672,
    /* 52 */ 673,
    /* 53 */ 674,
    /* 54 */ 675,
    /* 55 */ 676,
    /* 56 */ 677,
    /* 57 */ 679,
    /* 58 */ 680,
    /* 59 */ 681,
    /* 60 */ 682,
    /* 61 */ 683,
    /* 62 */ 684,
    /* 63 */ 685,
    /* 64 */ 746,
    /* 65 */ 755,
    /* 66 */ 756,
    /* 67 */ 760,
    /* 68 */ 832,
    /* 69 */ 839,
    /* 70 */ 842,
    /* 71 */ 844,
    /* 72 */ 850,
    /* 73 */ 851,
    /* 74 */ 853,
    /* 75 */ 854,
    /* 76 */ 858,
    /* 77 */ 861,
    /* 78 */ 862,
    /* 79 */ 864,
    /* 80 */ 865,
    /* 81 */ 868,
    /* 82 */ 869,
    /* 83 */ 870,
    /* 84 */ 873,
    /* 85 */ 875,
    /* 86 */ 878,
    /* 87 */ 880,
    /* 88 */ 882,
    /* 89 */ 889,
    /* 90 */ 894,
    /* 91 */ 968,
    /* 92 */ 970,
    /* 93 */ 972,
    /* 94 */ 982,
    /* 95 */ 983,
    /* 96 */ 984,
    /* 97 */ 986,
    /* 98 */ 989,
    /* 99 */ 990,
    /* 100 */ 991,
    /* 101 */ 992,
    /* 102 */ 999,
    /* 103 */ 1000,
    /* 104 */ 1001,
    /* 105 */ 1005,
    /* 106 */ 1006,
    /* 107 */ 1009,
    /* 108 */ 1011,
    /* 109 */ 1238,
    /* 110 */ 1448,
    /* 111 */ 1449,
    /* 112 */ 8387,
    /* 113 */ 8388,
    /* 114 */ 8390,
    /* 115 */ 8391,
    /* 116 */ 8392,
    /* 117 */ 8393,
    /* 118 */ 8394,
    /* 119 */ 8396,
    /* 120 */ 8397,
    /* 121 */ 8398,
    /* 122 */ 8399,
    /* 123 */ 8417,
    /* 124 */ 8419,
    /* 125 */ 8424,
    /* 126 */ 8426,
    /* 127 */ 8428,
    /* 128 */ 8430,
    /* 129 */ 8482,
    /* 130 */ 8483,
    /* 131 */ 8485,
    /* 132 */ 8486,
    /* 133 */ 8487,
    /* 134 */ 8488,
    /* 135 */ 8489,
    /* 136 */ 8491,
    /* 137 */ 8493,
    /* 138 */ 9600,
    /* 139 */ 9604,
    /* 140 */ 9606,
    /* 141 */ 9607,
    /* 142 */ 9608,
    /* 143 */ 9609,
    /* 144 */ 10088,
    /* 145 */ 10089,
    /* 146 */ 10090,
    /* 147 */ 10091,
    /* 148 */ 10110,
    /* 149 */ 10111,
    /* 150 */ 10112,
    /* 151 */ 10158,
    /* 152 */ 10237,
    /* 153 */ 10240,
    /* 154 */ 10248,
    /* 155 */ 10249,
    /* 156 */ 10250,
    /* 157 */ 10251,
    /* 158 */ 10252,
    /* 159 */ 10593,
    /* 160 */ 10594,
    /* 161 */ 10599,
    /* 162 */ 10600,
    /* 163 */ 10635,
    /* 164 */ 10677,
    /* 165 */ 10684,
    /* 166 */ 10691,
    /* 167 */ 10702,
    /* 168 */ 10705,
    /* 169 */ 10731,
    /* 170 */ 10814,
    /* 171 */ 10816,
    /* 172 */ 10827,
    /* 173 */ 10829,
    /* 174 */ 10830,
    /* 175 */ 10831,
    /* 176 */ 10838,
    /* 177 */ 10853,
    /* 178 */ 10874,
    /* 179 */ 10875,
    /* 180 */ 10884,
    /* 181 */ 10886,
    /* 182 */ 10893,
    /* 183 */ 10894,
    /* 184 */ 10897,
    /* 185 */ 10899,
    /* 186 */ 10904,
    /* 187 */ 10910,
    /* 188 */ 10911,
    /* 189 */ 10918,
    /* 190 */ 10920,
    /* 191 */ 10933,
    /* 192 */ 10936,
    /* 193 */ 10937,
    /* 194 */ 10938,
    /* 195 */ 10949,
    /* 196 */ 10959,
    /* 197 */ 10965,
    /* 198 */ 10966,
    /* 199 */ 10967,
    /* 200 */ 10969,
    /* 201 */ 10970,
    /* 202 */ 10988,
    /* 203 */ 10990,
    /* 204 */ 11005,
    /* 205 */ 11027,
    /* 206 */ 11028,
    /* 207 */ 11053,
    /* 208 */ 11058,
    /* 209 */ 11089,
    /* 210 */ 11090,
    /* 211 */ 11732,
    /* 212 */ 11746,
    /* 213 */ 11833,
    /* 214 */ 11834,
    /* 215 */ 12056,
    /* 216 */ 12057,
    /* 217 */ 12064,
    /* 218 */ 12065,
    /* 219 */ 12066,
    /* 220 */ 12072,
    /* 221 */ 12143,
    /* 222 */ 12449,
    /* 223 */ 12450,
    /* 224 */ 13219,
    /* 225 */ 13223,
    /* 226 */ 13233,
    /* 227 */ 13293,
    /* 228 */ 13294,
    /* 229 */ 13295,
    /* 230 */ 13296,
    /* 231 */ 14177,
    /* 232 */ 14198,
    /* 233 */ 14200,
    /* 234 */ 14202
};

void ves_icall_System_Array_InternalCreate (int, int, int, int, int); 
int ves_icall_System_Array_GetCorElementTypeOfElementTypeInternal (int); 
int ves_icall_System_Array_IsValueOfElementTypeInternal (int, int); 
int ves_icall_System_Array_CanChangePrimitive (int, int, int); 
int ves_icall_System_Array_FastCopy (int, int, int, int, int); 
int ves_icall_System_Array_GetLengthInternal_raw (int, int, int); 
int ves_icall_System_Array_GetLowerBoundInternal_raw (int, int, int); 
void ves_icall_System_Array_GetGenericValue_icall (int, int, int); 
void ves_icall_System_Array_GetValueImpl_raw (int, int, int, int); 
void ves_icall_System_Array_SetGenericValue_icall (int, int, int); 
void ves_icall_System_Array_SetValueImpl_raw (int, int, int, int); 
void ves_icall_System_Array_SetValueRelaxedImpl_raw (int, int, int, int); 
void ves_icall_System_Runtime_RuntimeImports_ZeroMemory (int, int); 
void ves_icall_System_Runtime_RuntimeImports_Memmove (int, int, int); 
void ves_icall_System_Buffer_BulkMoveWithWriteBarrier (int, int, int, int); 
int ves_icall_System_Delegate_AllocDelegateLike_internal_raw (int, int); 
int ves_icall_System_Delegate_CreateDelegate_internal_raw (int, int, int, int, int); 
int ves_icall_System_Delegate_GetVirtualMethod_internal_raw (int, int); 
void ves_icall_System_Enum_GetEnumValuesAndNames_raw (int, int, int, int); 
int ves_icall_System_Enum_InternalGetCorElementType (int); 
void ves_icall_System_Enum_InternalGetUnderlyingType_raw (int, int, int); 
int ves_icall_System_Environment_get_ProcessorCount (); 
void ves_icall_System_Environment_FailFast_raw (int, int, int, int); 
int ves_icall_System_GC_GetCollectionCount (int); 
void ves_icall_System_GC_register_ephemeron_array_raw (int, int); 
int ves_icall_System_GC_get_ephemeron_tombstone_raw (int); 
void ves_icall_System_GC_SuppressFinalize_raw (int, int); 
void ves_icall_System_GC_ReRegisterForFinalize_raw (int, int); 
void ves_icall_System_GC_GetGCMemoryInfo (int, int, int, int, int, int); 
int ves_icall_System_GC_AllocPinnedArray_raw (int, int, int); 
int ves_icall_System_Object_MemberwiseClone_raw (int, int); 
double ves_icall_System_Math_Ceiling (double); 
double ves_icall_System_Math_Cos (double); 
double ves_icall_System_Math_Floor (double); 
double ves_icall_System_Math_Log (double); 
double ves_icall_System_Math_Pow (double, double); 
double ves_icall_System_Math_Sin (double); 
double ves_icall_System_Math_Sqrt (double); 
double ves_icall_System_Math_Tan (double); 
double ves_icall_System_Math_ModF (double, int); 
float ves_icall_System_MathF_Acos (float); 
float ves_icall_System_MathF_Acosh (float); 
float ves_icall_System_MathF_Asin (float); 
float ves_icall_System_MathF_Asinh (float); 
float ves_icall_System_MathF_Atan (float); 
float ves_icall_System_MathF_Atan2 (float, float); 
float ves_icall_System_MathF_Atanh (float); 
float ves_icall_System_MathF_Cbrt (float); 
float ves_icall_System_MathF_Ceiling (float); 
float ves_icall_System_MathF_Cos (float); 
float ves_icall_System_MathF_Cosh (float); 
float ves_icall_System_MathF_Exp (float); 
float ves_icall_System_MathF_Floor (float); 
float ves_icall_System_MathF_Log (float); 
float ves_icall_System_MathF_Log10 (float); 
float ves_icall_System_MathF_Pow (float, float); 
float ves_icall_System_MathF_Sin (float); 
float ves_icall_System_MathF_Sinh (float); 
float ves_icall_System_MathF_Sqrt (float); 
float ves_icall_System_MathF_Tan (float); 
float ves_icall_System_MathF_Tanh (float); 
float ves_icall_System_MathF_FusedMultiplyAdd (float, float, float); 
float ves_icall_System_MathF_Log2 (float); 
float ves_icall_System_MathF_ModF (float, int); 
int ves_icall_RuntimeMethodHandle_GetFunctionPointer_raw (int, int); 
void ves_icall_RuntimeMethodHandle_ReboxFromNullable_raw (int, int, int); 
void ves_icall_RuntimeMethodHandle_ReboxToNullable_raw (int, int, int, int); 
void ves_icall_RuntimeType_GetParentType_raw (int, int, int); 
int ves_icall_RuntimeType_GetCorrespondingInflatedMethod_raw (int, int, int); 
void ves_icall_RuntimeType_make_array_type_raw (int, int, int, int); 
void ves_icall_RuntimeType_make_byref_type_raw (int, int, int); 
void ves_icall_RuntimeType_make_pointer_type_raw (int, int, int); 
void ves_icall_RuntimeType_MakeGenericType_raw (int, int, int, int); 
int ves_icall_RuntimeType_GetMethodsByName_native_raw (int, int, int, int, int); 
int ves_icall_RuntimeType_GetPropertiesByName_native_raw (int, int, int, int, int); 
int ves_icall_RuntimeType_GetConstructors_native_raw (int, int, int); 
void ves_icall_RuntimeType_GetPacking_raw (int, int, int, int); 
int ves_icall_System_RuntimeType_CreateInstanceInternal_raw (int, int); 
void ves_icall_RuntimeType_GetDeclaringMethod_raw (int, int, int); 
void ves_icall_System_RuntimeType_getFullName_raw (int, int, int, int, int); 
void ves_icall_RuntimeType_GetGenericArgumentsInternal_raw (int, int, int, int); 
int ves_icall_RuntimeType_GetGenericParameterPosition (int); 
int ves_icall_RuntimeType_GetEvents_native_raw (int, int, int, int); 
int ves_icall_RuntimeType_GetFields_native_raw (int, int, int, int, int); 
void ves_icall_RuntimeType_GetInterfaces_raw (int, int, int); 
int ves_icall_RuntimeType_GetNestedTypes_native_raw (int, int, int, int, int); 
void ves_icall_RuntimeType_GetDeclaringType_raw (int, int, int); 
void ves_icall_RuntimeType_GetName_raw (int, int, int); 
void ves_icall_RuntimeType_GetNamespace_raw (int, int, int); 
int ves_icall_RuntimeType_IsUnmanagedFunctionPointerInternal (int); 
int ves_icall_RuntimeType_FunctionPointerReturnAndParameterTypes_raw (int, int); 
int ves_icall_RuntimeTypeHandle_GetAttributes (int); 
int ves_icall_RuntimeTypeHandle_GetMetadataToken_raw (int, int); 
void ves_icall_RuntimeTypeHandle_GetGenericTypeDefinition_impl_raw (int, int, int); 
int ves_icall_RuntimeTypeHandle_GetCorElementType (int); 
int ves_icall_RuntimeTypeHandle_HasInstantiation (int); 
int ves_icall_RuntimeTypeHandle_IsInstanceOfType_raw (int, int, int); 
int ves_icall_RuntimeTypeHandle_HasReferences_raw (int, int); 
int ves_icall_RuntimeTypeHandle_GetArrayRank_raw (int, int); 
void ves_icall_RuntimeTypeHandle_GetAssembly_raw (int, int, int); 
void ves_icall_RuntimeTypeHandle_GetElementType_raw (int, int, int); 
void ves_icall_RuntimeTypeHandle_GetModule_raw (int, int, int); 
int ves_icall_RuntimeTypeHandle_type_is_assignable_from_raw (int, int, int); 
int ves_icall_RuntimeTypeHandle_IsGenericTypeDefinition (int); 
int ves_icall_RuntimeTypeHandle_GetGenericParameterInfo_raw (int, int); 
int ves_icall_RuntimeTypeHandle_is_subclass_of_raw (int, int, int); 
int ves_icall_RuntimeTypeHandle_IsByRefLike_raw (int, int); 
void ves_icall_System_RuntimeTypeHandle_internal_from_name_raw (int, int, int, int, int, int); 
int ves_icall_System_String_FastAllocateString_raw (int, int); 
int ves_icall_System_Type_internal_from_handle_raw (int, int); 
int ves_icall_System_ValueType_InternalGetHashCode_raw (int, int, int); 
int ves_icall_System_ValueType_Equals_raw (int, int, int, int); 
int ves_icall_System_Threading_Interlocked_CompareExchange_Int (int, int, int); 
void ves_icall_System_Threading_Interlocked_CompareExchange_Object (int, int, int, int); 
int ves_icall_System_Threading_Interlocked_Decrement_Int (int); 
int ves_icall_System_Threading_Interlocked_Increment_Int (int); 
int64_t ves_icall_System_Threading_Interlocked_Increment_Long (int); 
int ves_icall_System_Threading_Interlocked_Exchange_Int (int, int); 
void ves_icall_System_Threading_Interlocked_Exchange_Object (int, int, int); 
int64_t ves_icall_System_Threading_Interlocked_CompareExchange_Long (int, int64_t, int64_t); 
int64_t ves_icall_System_Threading_Interlocked_Exchange_Long (int, int64_t); 
int ves_icall_System_Threading_Interlocked_Add_Int (int, int); 
int64_t ves_icall_System_Threading_Interlocked_Add_Long (int, int64_t); 
void ves_icall_System_Threading_Monitor_Monitor_Enter_raw (int, int); 
void mono_monitor_exit_icall_raw (int, int); 
void ves_icall_System_Threading_Monitor_Monitor_pulse_raw (int, int); 
void ves_icall_System_Threading_Monitor_Monitor_pulse_all_raw (int, int); 
int ves_icall_System_Threading_Monitor_Monitor_wait_raw (int, int, int, int); 
void ves_icall_System_Threading_Monitor_Monitor_try_enter_with_atomic_var_raw (int, int, int, int, int); 
void ves_icall_System_Threading_Thread_InitInternal_raw (int, int); 
int ves_icall_System_Threading_Thread_GetCurrentThread (); 
void ves_icall_System_Threading_InternalThread_Thread_free_internal_raw (int, int); 
int ves_icall_System_Threading_Thread_GetState_raw (int, int); 
void ves_icall_System_Threading_Thread_SetState_raw (int, int, int); 
void ves_icall_System_Threading_Thread_ClrState_raw (int, int, int); 
void ves_icall_System_Threading_Thread_SetName_icall_raw (int, int, int, int); 
int ves_icall_System_Threading_Thread_YieldInternal (); 
void ves_icall_System_Threading_Thread_SetPriority_raw (int, int, int); 
void ves_icall_System_Runtime_Loader_AssemblyLoadContext_PrepareForAssemblyLoadContextRelease_raw (int, int, int); 
int ves_icall_System_Runtime_Loader_AssemblyLoadContext_GetLoadContextForAssembly_raw (int, int); 
int ves_icall_System_Runtime_Loader_AssemblyLoadContext_InternalLoadFile_raw (int, int, int, int); 
int ves_icall_System_Runtime_Loader_AssemblyLoadContext_InternalInitializeNativeALC_raw (int, int, int, int, int); 
int ves_icall_System_Runtime_Loader_AssemblyLoadContext_InternalLoadFromStream_raw (int, int, int, int, int, int); 
int ves_icall_System_Runtime_Loader_AssemblyLoadContext_InternalGetLoadedAssemblies_raw (int); 
int ves_icall_System_GCHandle_InternalAlloc_raw (int, int, int); 
void ves_icall_System_GCHandle_InternalFree_raw (int, int); 
int ves_icall_System_GCHandle_InternalGet_raw (int, int); 
void ves_icall_System_GCHandle_InternalSet_raw (int, int, int); 
int ves_icall_System_Runtime_InteropServices_Marshal_GetLastPInvokeError (); 
void ves_icall_System_Runtime_InteropServices_Marshal_SetLastPInvokeError (int); 
void ves_icall_System_Runtime_InteropServices_Marshal_StructureToPtr_raw (int, int, int, int); 
int ves_icall_System_Runtime_InteropServices_NativeLibrary_LoadByName_raw (int, int, int, int, int, int); 
int ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_InternalGetHashCode_raw (int, int); 
int ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_GetObjectValue_raw (int, int); 
int ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_GetUninitializedObjectInternal_raw (int, int); 
void ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_InitializeArray_raw (int, int, int); 
int ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_GetSpanDataFrom_raw (int, int, int, int); 
int ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_SufficientExecutionStack (); 
int ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_InternalBox_raw (int, int, int); 
int ves_icall_System_Reflection_Assembly_GetExecutingAssembly_raw (int, int); 
int ves_icall_System_Reflection_Assembly_GetEntryAssembly_raw (int); 
int ves_icall_System_Reflection_Assembly_InternalLoad_raw (int, int, int, int); 
int ves_icall_System_Reflection_Assembly_InternalGetType_raw (int, int, int, int, int, int); 
int ves_icall_System_Reflection_AssemblyName_GetNativeName (int); 
int ves_icall_MonoCustomAttrs_GetCustomAttributesInternal_raw (int, int, int, int); 
int ves_icall_MonoCustomAttrs_GetCustomAttributesDataInternal_raw (int, int); 
int ves_icall_MonoCustomAttrs_IsDefinedInternal_raw (int, int, int); 
int ves_icall_System_Reflection_FieldInfo_internal_from_handle_type_raw (int, int, int); 
int ves_icall_System_Reflection_FieldInfo_get_marshal_info_raw (int, int); 
int ves_icall_System_Reflection_LoaderAllocatorScout_Destroy (int); 
void ves_icall_System_Reflection_RuntimeAssembly_GetManifestResourceNames_raw (int, int, int); 
void ves_icall_System_Reflection_RuntimeAssembly_GetExportedTypes_raw (int, int, int); 
void ves_icall_System_Reflection_RuntimeAssembly_GetInfo_raw (int, int, int, int); 
int ves_icall_System_Reflection_RuntimeAssembly_GetManifestResourceInternal_raw (int, int, int, int, int); 
void ves_icall_System_Reflection_Assembly_GetManifestModuleInternal_raw (int, int, int); 
void ves_icall_System_Reflection_RuntimeAssembly_GetModulesInternal_raw (int, int, int); 
void ves_icall_System_Reflection_RuntimeCustomAttributeData_ResolveArgumentsInternal_raw (int, int, int, int, int, int, int); 
void ves_icall_RuntimeEventInfo_get_event_info_raw (int, int, int); 
int ves_icall_reflection_get_token_raw (int, int); 
int ves_icall_System_Reflection_EventInfo_internal_from_handle_type_raw (int, int, int); 
int ves_icall_RuntimeFieldInfo_ResolveType_raw (int, int); 
int ves_icall_RuntimeFieldInfo_GetParentType_raw (int, int, int); 
int ves_icall_RuntimeFieldInfo_GetFieldOffset_raw (int, int); 
int ves_icall_RuntimeFieldInfo_GetValueInternal_raw (int, int, int); 
void ves_icall_RuntimeFieldInfo_SetValueInternal_raw (int, int, int, int); 
int ves_icall_RuntimeFieldInfo_GetRawConstantValue_raw (int, int); 
int ves_icall_reflection_get_token_raw (int, int); 
void ves_icall_get_method_info_raw (int, int, int); 
int ves_icall_get_method_attributes (int); 
int ves_icall_System_Reflection_MonoMethodInfo_get_parameter_info_raw (int, int, int); 
int ves_icall_System_MonoMethodInfo_get_retval_marshal_raw (int, int); 
int ves_icall_System_Reflection_RuntimeMethodInfo_GetMethodFromHandleInternalType_native_raw (int, int, int, int); 
int ves_icall_RuntimeMethodInfo_get_name_raw (int, int); 
int ves_icall_RuntimeMethodInfo_get_base_method_raw (int, int, int); 
int ves_icall_reflection_get_token_raw (int, int); 
int ves_icall_InternalInvoke_raw (int, int, int, int, int); 
void ves_icall_RuntimeMethodInfo_GetPInvoke_raw (int, int, int, int, int); 
int ves_icall_RuntimeMethodInfo_MakeGenericMethod_impl_raw (int, int, int); 
int ves_icall_RuntimeMethodInfo_GetGenericArguments_raw (int, int); 
int ves_icall_RuntimeMethodInfo_GetGenericMethodDefinition_raw (int, int); 
int ves_icall_RuntimeMethodInfo_get_IsGenericMethodDefinition_raw (int, int); 
int ves_icall_RuntimeMethodInfo_get_IsGenericMethod_raw (int, int); 
void ves_icall_InvokeClassConstructor_raw (int, int); 
int ves_icall_InternalInvoke_raw (int, int, int, int, int); 
int ves_icall_reflection_get_token_raw (int, int); 
void ves_icall_System_Reflection_RuntimeModule_GetGuidInternal_raw (int, int, int); 
int ves_icall_System_Reflection_RuntimeModule_ResolveMethodToken_raw (int, int, int, int, int, int); 
int ves_icall_RuntimeParameterInfo_GetTypeModifiers_raw (int, int, int, int, int, int); 
void ves_icall_RuntimePropertyInfo_get_property_info_raw (int, int, int, int); 
int ves_icall_reflection_get_token_raw (int, int); 
int ves_icall_System_Reflection_RuntimePropertyInfo_internal_from_handle_type_raw (int, int, int); 
int ves_icall_CustomAttributeBuilder_GetBlob_raw (int, int, int, int, int, int, int, int); 
void ves_icall_DynamicMethod_create_dynamic_method_raw (int, int, int, int, int); 
void ves_icall_AssemblyBuilder_basic_init_raw (int, int); 
void ves_icall_AssemblyBuilder_UpdateNativeCustomAttributes_raw (int, int); 
void ves_icall_ModuleBuilder_basic_init_raw (int, int); 
void ves_icall_ModuleBuilder_set_wrappers_type_raw (int, int, int); 
int ves_icall_ModuleBuilder_getUSIndex_raw (int, int, int); 
int ves_icall_ModuleBuilder_getToken_raw (int, int, int, int); 
int ves_icall_ModuleBuilder_getMethodToken_raw (int, int, int, int); 
void ves_icall_ModuleBuilder_RegisterToken_raw (int, int, int, int); 
int ves_icall_TypeBuilder_create_runtime_class_raw (int, int); 
int ves_icall_System_IO_Stream_HasOverriddenBeginEndRead_raw (int, int); 
int ves_icall_System_IO_Stream_HasOverriddenBeginEndWrite_raw (int, int); 
int ves_icall_System_Diagnostics_Debugger_IsAttached_internal (); 
int ves_icall_System_Diagnostics_StackFrame_GetFrameInfo (int, int, int, int, int, int, int, int); 
void ves_icall_System_Diagnostics_StackTrace_GetTrace (int, int, int, int); 
void ves_icall_System_Diagnostics_Tracing_NativeRuntimeEventSource_LogContentionStart (int, int, int, int, uint64_t); 
void ves_icall_System_Diagnostics_Tracing_NativeRuntimeEventSource_LogContentionStop (int, int, double); 
void ves_icall_System_Diagnostics_Tracing_NativeRuntimeEventSource_LogWaitHandleWaitStart (int, int, int); 
void ves_icall_System_Diagnostics_Tracing_NativeRuntimeEventSource_LogWaitHandleWaitStop (int); 
int ves_icall_Mono_RuntimeClassHandle_GetTypeFromClass (int); 
void ves_icall_Mono_RuntimeGPtrArrayHandle_GPtrArrayFree (int); 
int ves_icall_Mono_SafeStringMarshal_StringToUtf8 (int); 
void ves_icall_Mono_SafeStringMarshal_GFree (int);

static void *corlib_icall_funcs [] = {
    /* 0:220 */ ves_icall_System_Array_InternalCreate,
    /* 1:231 */ ves_icall_System_Array_GetCorElementTypeOfElementTypeInternal,
    /* 2:232 */ ves_icall_System_Array_IsValueOfElementTypeInternal,
    /* 3:233 */ ves_icall_System_Array_CanChangePrimitive,
    /* 4:234 */ ves_icall_System_Array_FastCopy,
    /* 5:235 */ ves_icall_System_Array_GetLengthInternal_raw,
    /* 6:236 */ ves_icall_System_Array_GetLowerBoundInternal_raw,
    /* 7:237 */ ves_icall_System_Array_GetGenericValue_icall,
    /* 8:238 */ ves_icall_System_Array_GetValueImpl_raw,
    /* 9:239 */ ves_icall_System_Array_SetGenericValue_icall,
    /* 10:242 */ ves_icall_System_Array_SetValueImpl_raw,
    /* 11:243 */ ves_icall_System_Array_SetValueRelaxedImpl_raw,
    /* 12:370 */ ves_icall_System_Runtime_RuntimeImports_ZeroMemory,
    /* 13:371 */ ves_icall_System_Runtime_RuntimeImports_Memmove,
    /* 14:372 */ ves_icall_System_Buffer_BulkMoveWithWriteBarrier,
    /* 15:400 */ ves_icall_System_Delegate_AllocDelegateLike_internal_raw,
    /* 16:401 */ ves_icall_System_Delegate_CreateDelegate_internal_raw,
    /* 17:402 */ ves_icall_System_Delegate_GetVirtualMethod_internal_raw,
    /* 18:429 */ ves_icall_System_Enum_GetEnumValuesAndNames_raw,
    /* 19:430 */ ves_icall_System_Enum_InternalGetCorElementType,
    /* 20:431 */ ves_icall_System_Enum_InternalGetUnderlyingType_raw,
    /* 21:531 */ ves_icall_System_Environment_get_ProcessorCount,
    /* 22:534 */ ves_icall_System_Environment_FailFast_raw,
    /* 23:574 */ ves_icall_System_GC_GetCollectionCount,
    /* 24:575 */ ves_icall_System_GC_register_ephemeron_array_raw,
    /* 25:576 */ ves_icall_System_GC_get_ephemeron_tombstone_raw,
    /* 26:579 */ ves_icall_System_GC_SuppressFinalize_raw,
    /* 27:581 */ ves_icall_System_GC_ReRegisterForFinalize_raw,
    /* 28:585 */ ves_icall_System_GC_GetGCMemoryInfo,
    /* 29:587 */ ves_icall_System_GC_AllocPinnedArray_raw,
    /* 30:592 */ ves_icall_System_Object_MemberwiseClone_raw,
    /* 31:600 */ ves_icall_System_Math_Ceiling,
    /* 32:601 */ ves_icall_System_Math_Cos,
    /* 33:602 */ ves_icall_System_Math_Floor,
    /* 34:603 */ ves_icall_System_Math_Log,
    /* 35:604 */ ves_icall_System_Math_Pow,
    /* 36:605 */ ves_icall_System_Math_Sin,
    /* 37:606 */ ves_icall_System_Math_Sqrt,
    /* 38:607 */ ves_icall_System_Math_Tan,
    /* 39:608 */ ves_icall_System_Math_ModF,
    /* 40:661 */ ves_icall_System_MathF_Acos,
    /* 41:662 */ ves_icall_System_MathF_Acosh,
    /* 42:663 */ ves_icall_System_MathF_Asin,
    /* 43:664 */ ves_icall_System_MathF_Asinh,
    /* 44:665 */ ves_icall_System_MathF_Atan,
    /* 45:666 */ ves_icall_System_MathF_Atan2,
    /* 46:667 */ ves_icall_System_MathF_Atanh,
    /* 47:668 */ ves_icall_System_MathF_Cbrt,
    /* 48:669 */ ves_icall_System_MathF_Ceiling,
    /* 49:670 */ ves_icall_System_MathF_Cos,
    /* 50:671 */ ves_icall_System_MathF_Cosh,
    /* 51:672 */ ves_icall_System_MathF_Exp,
    /* 52:673 */ ves_icall_System_MathF_Floor,
    /* 53:674 */ ves_icall_System_MathF_Log,
    /* 54:675 */ ves_icall_System_MathF_Log10,
    /* 55:676 */ ves_icall_System_MathF_Pow,
    /* 56:677 */ ves_icall_System_MathF_Sin,
    /* 57:679 */ ves_icall_System_MathF_Sinh,
    /* 58:680 */ ves_icall_System_MathF_Sqrt,
    /* 59:681 */ ves_icall_System_MathF_Tan,
    /* 60:682 */ ves_icall_System_MathF_Tanh,
    /* 61:683 */ ves_icall_System_MathF_FusedMultiplyAdd,
    /* 62:684 */ ves_icall_System_MathF_Log2,
    /* 63:685 */ ves_icall_System_MathF_ModF,
    /* 64:746 */ ves_icall_RuntimeMethodHandle_GetFunctionPointer_raw,
    /* 65:755 */ ves_icall_RuntimeMethodHandle_ReboxFromNullable_raw,
    /* 66:756 */ ves_icall_RuntimeMethodHandle_ReboxToNullable_raw,
    /* 67:760 */ ves_icall_RuntimeType_GetParentType_raw,
    /* 68:832 */ ves_icall_RuntimeType_GetCorrespondingInflatedMethod_raw,
    /* 69:839 */ ves_icall_RuntimeType_make_array_type_raw,
    /* 70:842 */ ves_icall_RuntimeType_make_byref_type_raw,
    /* 71:844 */ ves_icall_RuntimeType_make_pointer_type_raw,
    /* 72:850 */ ves_icall_RuntimeType_MakeGenericType_raw,
    /* 73:851 */ ves_icall_RuntimeType_GetMethodsByName_native_raw,
    /* 74:853 */ ves_icall_RuntimeType_GetPropertiesByName_native_raw,
    /* 75:854 */ ves_icall_RuntimeType_GetConstructors_native_raw,
    /* 76:858 */ ves_icall_RuntimeType_GetPacking_raw,
    /* 77:861 */ ves_icall_System_RuntimeType_CreateInstanceInternal_raw,
    /* 78:862 */ ves_icall_RuntimeType_GetDeclaringMethod_raw,
    /* 79:864 */ ves_icall_System_RuntimeType_getFullName_raw,
    /* 80:865 */ ves_icall_RuntimeType_GetGenericArgumentsInternal_raw,
    /* 81:868 */ ves_icall_RuntimeType_GetGenericParameterPosition,
    /* 82:869 */ ves_icall_RuntimeType_GetEvents_native_raw,
    /* 83:870 */ ves_icall_RuntimeType_GetFields_native_raw,
    /* 84:873 */ ves_icall_RuntimeType_GetInterfaces_raw,
    /* 85:875 */ ves_icall_RuntimeType_GetNestedTypes_native_raw,
    /* 86:878 */ ves_icall_RuntimeType_GetDeclaringType_raw,
    /* 87:880 */ ves_icall_RuntimeType_GetName_raw,
    /* 88:882 */ ves_icall_RuntimeType_GetNamespace_raw,
    /* 89:889 */ ves_icall_RuntimeType_IsUnmanagedFunctionPointerInternal,
    /* 90:894 */ ves_icall_RuntimeType_FunctionPointerReturnAndParameterTypes_raw,
    /* 91:968 */ ves_icall_RuntimeTypeHandle_GetAttributes,
    /* 92:970 */ ves_icall_RuntimeTypeHandle_GetMetadataToken_raw,
    /* 93:972 */ ves_icall_RuntimeTypeHandle_GetGenericTypeDefinition_impl_raw,
    /* 94:982 */ ves_icall_RuntimeTypeHandle_GetCorElementType,
    /* 95:983 */ ves_icall_RuntimeTypeHandle_HasInstantiation,
    /* 96:984 */ ves_icall_RuntimeTypeHandle_IsInstanceOfType_raw,
    /* 97:986 */ ves_icall_RuntimeTypeHandle_HasReferences_raw,
    /* 98:989 */ ves_icall_RuntimeTypeHandle_GetArrayRank_raw,
    /* 99:990 */ ves_icall_RuntimeTypeHandle_GetAssembly_raw,
    /* 100:991 */ ves_icall_RuntimeTypeHandle_GetElementType_raw,
    /* 101:992 */ ves_icall_RuntimeTypeHandle_GetModule_raw,
    /* 102:999 */ ves_icall_RuntimeTypeHandle_type_is_assignable_from_raw,
    /* 103:1000 */ ves_icall_RuntimeTypeHandle_IsGenericTypeDefinition,
    /* 104:1001 */ ves_icall_RuntimeTypeHandle_GetGenericParameterInfo_raw,
    /* 105:1005 */ ves_icall_RuntimeTypeHandle_is_subclass_of_raw,
    /* 106:1006 */ ves_icall_RuntimeTypeHandle_IsByRefLike_raw,
    /* 107:1009 */ ves_icall_System_RuntimeTypeHandle_internal_from_name_raw,
    /* 108:1011 */ ves_icall_System_String_FastAllocateString_raw,
    /* 109:1238 */ ves_icall_System_Type_internal_from_handle_raw,
    /* 110:1448 */ ves_icall_System_ValueType_InternalGetHashCode_raw,
    /* 111:1449 */ ves_icall_System_ValueType_Equals_raw,
    /* 112:8387 */ ves_icall_System_Threading_Interlocked_CompareExchange_Int,
    /* 113:8388 */ ves_icall_System_Threading_Interlocked_CompareExchange_Object,
    /* 114:8390 */ ves_icall_System_Threading_Interlocked_Decrement_Int,
    /* 115:8391 */ ves_icall_System_Threading_Interlocked_Increment_Int,
    /* 116:8392 */ ves_icall_System_Threading_Interlocked_Increment_Long,
    /* 117:8393 */ ves_icall_System_Threading_Interlocked_Exchange_Int,
    /* 118:8394 */ ves_icall_System_Threading_Interlocked_Exchange_Object,
    /* 119:8396 */ ves_icall_System_Threading_Interlocked_CompareExchange_Long,
    /* 120:8397 */ ves_icall_System_Threading_Interlocked_Exchange_Long,
    /* 121:8398 */ ves_icall_System_Threading_Interlocked_Add_Int,
    /* 122:8399 */ ves_icall_System_Threading_Interlocked_Add_Long,
    /* 123:8417 */ ves_icall_System_Threading_Monitor_Monitor_Enter_raw,
    /* 124:8419 */ mono_monitor_exit_icall_raw,
    /* 125:8424 */ ves_icall_System_Threading_Monitor_Monitor_pulse_raw,
    /* 126:8426 */ ves_icall_System_Threading_Monitor_Monitor_pulse_all_raw,
    /* 127:8428 */ ves_icall_System_Threading_Monitor_Monitor_wait_raw,
    /* 128:8430 */ ves_icall_System_Threading_Monitor_Monitor_try_enter_with_atomic_var_raw,
    /* 129:8482 */ ves_icall_System_Threading_Thread_InitInternal_raw,
    /* 130:8483 */ ves_icall_System_Threading_Thread_GetCurrentThread,
    /* 131:8485 */ ves_icall_System_Threading_InternalThread_Thread_free_internal_raw,
    /* 132:8486 */ ves_icall_System_Threading_Thread_GetState_raw,
    /* 133:8487 */ ves_icall_System_Threading_Thread_SetState_raw,
    /* 134:8488 */ ves_icall_System_Threading_Thread_ClrState_raw,
    /* 135:8489 */ ves_icall_System_Threading_Thread_SetName_icall_raw,
    /* 136:8491 */ ves_icall_System_Threading_Thread_YieldInternal,
    /* 137:8493 */ ves_icall_System_Threading_Thread_SetPriority_raw,
    /* 138:9600 */ ves_icall_System_Runtime_Loader_AssemblyLoadContext_PrepareForAssemblyLoadContextRelease_raw,
    /* 139:9604 */ ves_icall_System_Runtime_Loader_AssemblyLoadContext_GetLoadContextForAssembly_raw,
    /* 140:9606 */ ves_icall_System_Runtime_Loader_AssemblyLoadContext_InternalLoadFile_raw,
    /* 141:9607 */ ves_icall_System_Runtime_Loader_AssemblyLoadContext_InternalInitializeNativeALC_raw,
    /* 142:9608 */ ves_icall_System_Runtime_Loader_AssemblyLoadContext_InternalLoadFromStream_raw,
    /* 143:9609 */ ves_icall_System_Runtime_Loader_AssemblyLoadContext_InternalGetLoadedAssemblies_raw,
    /* 144:10088 */ ves_icall_System_GCHandle_InternalAlloc_raw,
    /* 145:10089 */ ves_icall_System_GCHandle_InternalFree_raw,
    /* 146:10090 */ ves_icall_System_GCHandle_InternalGet_raw,
    /* 147:10091 */ ves_icall_System_GCHandle_InternalSet_raw,
    /* 148:10110 */ ves_icall_System_Runtime_InteropServices_Marshal_GetLastPInvokeError,
    /* 149:10111 */ ves_icall_System_Runtime_InteropServices_Marshal_SetLastPInvokeError,
    /* 150:10112 */ ves_icall_System_Runtime_InteropServices_Marshal_StructureToPtr_raw,
    /* 151:10158 */ ves_icall_System_Runtime_InteropServices_NativeLibrary_LoadByName_raw,
    /* 152:10237 */ ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_InternalGetHashCode_raw,
    /* 153:10240 */ ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_GetObjectValue_raw,
    /* 154:10248 */ ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_GetUninitializedObjectInternal_raw,
    /* 155:10249 */ ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_InitializeArray_raw,
    /* 156:10250 */ ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_GetSpanDataFrom_raw,
    /* 157:10251 */ ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_SufficientExecutionStack,
    /* 158:10252 */ ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_InternalBox_raw,
    /* 159:10593 */ ves_icall_System_Reflection_Assembly_GetExecutingAssembly_raw,
    /* 160:10594 */ ves_icall_System_Reflection_Assembly_GetEntryAssembly_raw,
    /* 161:10599 */ ves_icall_System_Reflection_Assembly_InternalLoad_raw,
    /* 162:10600 */ ves_icall_System_Reflection_Assembly_InternalGetType_raw,
    /* 163:10635 */ ves_icall_System_Reflection_AssemblyName_GetNativeName,
    /* 164:10677 */ ves_icall_MonoCustomAttrs_GetCustomAttributesInternal_raw,
    /* 165:10684 */ ves_icall_MonoCustomAttrs_GetCustomAttributesDataInternal_raw,
    /* 166:10691 */ ves_icall_MonoCustomAttrs_IsDefinedInternal_raw,
    /* 167:10702 */ ves_icall_System_Reflection_FieldInfo_internal_from_handle_type_raw,
    /* 168:10705 */ ves_icall_System_Reflection_FieldInfo_get_marshal_info_raw,
    /* 169:10731 */ ves_icall_System_Reflection_LoaderAllocatorScout_Destroy,
    /* 170:10814 */ ves_icall_System_Reflection_RuntimeAssembly_GetManifestResourceNames_raw,
    /* 171:10816 */ ves_icall_System_Reflection_RuntimeAssembly_GetExportedTypes_raw,
    /* 172:10827 */ ves_icall_System_Reflection_RuntimeAssembly_GetInfo_raw,
    /* 173:10829 */ ves_icall_System_Reflection_RuntimeAssembly_GetManifestResourceInternal_raw,
    /* 174:10830 */ ves_icall_System_Reflection_Assembly_GetManifestModuleInternal_raw,
    /* 175:10831 */ ves_icall_System_Reflection_RuntimeAssembly_GetModulesInternal_raw,
    /* 176:10838 */ ves_icall_System_Reflection_RuntimeCustomAttributeData_ResolveArgumentsInternal_raw,
    /* 177:10853 */ ves_icall_RuntimeEventInfo_get_event_info_raw,
    /* 178:10874 */ ves_icall_reflection_get_token_raw,
    /* 179:10875 */ ves_icall_System_Reflection_EventInfo_internal_from_handle_type_raw,
    /* 180:10884 */ ves_icall_RuntimeFieldInfo_ResolveType_raw,
    /* 181:10886 */ ves_icall_RuntimeFieldInfo_GetParentType_raw,
    /* 182:10893 */ ves_icall_RuntimeFieldInfo_GetFieldOffset_raw,
    /* 183:10894 */ ves_icall_RuntimeFieldInfo_GetValueInternal_raw,
    /* 184:10897 */ ves_icall_RuntimeFieldInfo_SetValueInternal_raw,
    /* 185:10899 */ ves_icall_RuntimeFieldInfo_GetRawConstantValue_raw,
    /* 186:10904 */ ves_icall_reflection_get_token_raw,
    /* 187:10910 */ ves_icall_get_method_info_raw,
    /* 188:10911 */ ves_icall_get_method_attributes,
    /* 189:10918 */ ves_icall_System_Reflection_MonoMethodInfo_get_parameter_info_raw,
    /* 190:10920 */ ves_icall_System_MonoMethodInfo_get_retval_marshal_raw,
    /* 191:10933 */ ves_icall_System_Reflection_RuntimeMethodInfo_GetMethodFromHandleInternalType_native_raw,
    /* 192:10936 */ ves_icall_RuntimeMethodInfo_get_name_raw,
    /* 193:10937 */ ves_icall_RuntimeMethodInfo_get_base_method_raw,
    /* 194:10938 */ ves_icall_reflection_get_token_raw,
    /* 195:10949 */ ves_icall_InternalInvoke_raw,
    /* 196:10959 */ ves_icall_RuntimeMethodInfo_GetPInvoke_raw,
    /* 197:10965 */ ves_icall_RuntimeMethodInfo_MakeGenericMethod_impl_raw,
    /* 198:10966 */ ves_icall_RuntimeMethodInfo_GetGenericArguments_raw,
    /* 199:10967 */ ves_icall_RuntimeMethodInfo_GetGenericMethodDefinition_raw,
    /* 200:10969 */ ves_icall_RuntimeMethodInfo_get_IsGenericMethodDefinition_raw,
    /* 201:10970 */ ves_icall_RuntimeMethodInfo_get_IsGenericMethod_raw,
    /* 202:10988 */ ves_icall_InvokeClassConstructor_raw,
    /* 203:10990 */ ves_icall_InternalInvoke_raw,
    /* 204:11005 */ ves_icall_reflection_get_token_raw,
    /* 205:11027 */ ves_icall_System_Reflection_RuntimeModule_GetGuidInternal_raw,
    /* 206:11028 */ ves_icall_System_Reflection_RuntimeModule_ResolveMethodToken_raw,
    /* 207:11053 */ ves_icall_RuntimeParameterInfo_GetTypeModifiers_raw,
    /* 208:11058 */ ves_icall_RuntimePropertyInfo_get_property_info_raw,
    /* 209:11089 */ ves_icall_reflection_get_token_raw,
    /* 210:11090 */ ves_icall_System_Reflection_RuntimePropertyInfo_internal_from_handle_type_raw,
    /* 211:11732 */ ves_icall_CustomAttributeBuilder_GetBlob_raw,
    /* 212:11746 */ ves_icall_DynamicMethod_create_dynamic_method_raw,
    /* 213:11833 */ ves_icall_AssemblyBuilder_basic_init_raw,
    /* 214:11834 */ ves_icall_AssemblyBuilder_UpdateNativeCustomAttributes_raw,
    /* 215:12056 */ ves_icall_ModuleBuilder_basic_init_raw,
    /* 216:12057 */ ves_icall_ModuleBuilder_set_wrappers_type_raw,
    /* 217:12064 */ ves_icall_ModuleBuilder_getUSIndex_raw,
    /* 218:12065 */ ves_icall_ModuleBuilder_getToken_raw,
    /* 219:12066 */ ves_icall_ModuleBuilder_getMethodToken_raw,
    /* 220:12072 */ ves_icall_ModuleBuilder_RegisterToken_raw,
    /* 221:12143 */ ves_icall_TypeBuilder_create_runtime_class_raw,
    /* 222:12449 */ ves_icall_System_IO_Stream_HasOverriddenBeginEndRead_raw,
    /* 223:12450 */ ves_icall_System_IO_Stream_HasOverriddenBeginEndWrite_raw,
    /* 224:13219 */ ves_icall_System_Diagnostics_Debugger_IsAttached_internal,
    /* 225:13223 */ ves_icall_System_Diagnostics_StackFrame_GetFrameInfo,
    /* 226:13233 */ ves_icall_System_Diagnostics_StackTrace_GetTrace,
    /* 227:13293 */ ves_icall_System_Diagnostics_Tracing_NativeRuntimeEventSource_LogContentionStart,
    /* 228:13294 */ ves_icall_System_Diagnostics_Tracing_NativeRuntimeEventSource_LogContentionStop,
    /* 229:13295 */ ves_icall_System_Diagnostics_Tracing_NativeRuntimeEventSource_LogWaitHandleWaitStart,
    /* 230:13296 */ ves_icall_System_Diagnostics_Tracing_NativeRuntimeEventSource_LogWaitHandleWaitStop,
    /* 231:14177 */ ves_icall_Mono_RuntimeClassHandle_GetTypeFromClass,
    /* 232:14198 */ ves_icall_Mono_RuntimeGPtrArrayHandle_GPtrArrayFree,
    /* 233:14200 */ ves_icall_Mono_SafeStringMarshal_StringToUtf8,
    /* 234:14202 */ ves_icall_Mono_SafeStringMarshal_GFree
};

static uint8_t corlib_icall_flags [] = {
    /* 0:220 */ 0,
    /* 1:231 */ 0,
    /* 2:232 */ 0,
    /* 3:233 */ 0,
    /* 4:234 */ 0,
    /* 5:235 */ 4,
    /* 6:236 */ 4,
    /* 7:237 */ 0,
    /* 8:238 */ 4,
    /* 9:239 */ 0,
    /* 10:242 */ 4,
    /* 11:243 */ 4,
    /* 12:370 */ 0,
    /* 13:371 */ 0,
    /* 14:372 */ 0,
    /* 15:400 */ 4,
    /* 16:401 */ 4,
    /* 17:402 */ 4,
    /* 18:429 */ 4,
    /* 19:430 */ 0,
    /* 20:431 */ 4,
    /* 21:531 */ 0,
    /* 22:534 */ 4,
    /* 23:574 */ 0,
    /* 24:575 */ 4,
    /* 25:576 */ 4,
    /* 26:579 */ 4,
    /* 27:581 */ 4,
    /* 28:585 */ 0,
    /* 29:587 */ 4,
    /* 30:592 */ 4,
    /* 31:600 */ 0,
    /* 32:601 */ 0,
    /* 33:602 */ 0,
    /* 34:603 */ 0,
    /* 35:604 */ 0,
    /* 36:605 */ 0,
    /* 37:606 */ 0,
    /* 38:607 */ 0,
    /* 39:608 */ 0,
    /* 40:661 */ 0,
    /* 41:662 */ 0,
    /* 42:663 */ 0,
    /* 43:664 */ 0,
    /* 44:665 */ 0,
    /* 45:666 */ 0,
    /* 46:667 */ 0,
    /* 47:668 */ 0,
    /* 48:669 */ 0,
    /* 49:670 */ 0,
    /* 50:671 */ 0,
    /* 51:672 */ 0,
    /* 52:673 */ 0,
    /* 53:674 */ 0,
    /* 54:675 */ 0,
    /* 55:676 */ 0,
    /* 56:677 */ 0,
    /* 57:679 */ 0,
    /* 58:680 */ 0,
    /* 59:681 */ 0,
    /* 60:682 */ 0,
    /* 61:683 */ 0,
    /* 62:684 */ 0,
    /* 63:685 */ 0,
    /* 64:746 */ 4,
    /* 65:755 */ 4,
    /* 66:756 */ 4,
    /* 67:760 */ 4,
    /* 68:832 */ 4,
    /* 69:839 */ 4,
    /* 70:842 */ 4,
    /* 71:844 */ 4,
    /* 72:850 */ 4,
    /* 73:851 */ 4,
    /* 74:853 */ 4,
    /* 75:854 */ 4,
    /* 76:858 */ 4,
    /* 77:861 */ 4,
    /* 78:862 */ 4,
    /* 79:864 */ 4,
    /* 80:865 */ 4,
    /* 81:868 */ 0,
    /* 82:869 */ 4,
    /* 83:870 */ 4,
    /* 84:873 */ 4,
    /* 85:875 */ 4,
    /* 86:878 */ 4,
    /* 87:880 */ 4,
    /* 88:882 */ 4,
    /* 89:889 */ 0,
    /* 90:894 */ 4,
    /* 91:968 */ 0,
    /* 92:970 */ 4,
    /* 93:972 */ 4,
    /* 94:982 */ 0,
    /* 95:983 */ 0,
    /* 96:984 */ 4,
    /* 97:986 */ 4,
    /* 98:989 */ 4,
    /* 99:990 */ 4,
    /* 100:991 */ 4,
    /* 101:992 */ 4,
    /* 102:999 */ 4,
    /* 103:1000 */ 0,
    /* 104:1001 */ 4,
    /* 105:1005 */ 4,
    /* 106:1006 */ 4,
    /* 107:1009 */ 4,
    /* 108:1011 */ 4,
    /* 109:1238 */ 4,
    /* 110:1448 */ 4,
    /* 111:1449 */ 4,
    /* 112:8387 */ 0,
    /* 113:8388 */ 0,
    /* 114:8390 */ 0,
    /* 115:8391 */ 0,
    /* 116:8392 */ 0,
    /* 117:8393 */ 0,
    /* 118:8394 */ 0,
    /* 119:8396 */ 0,
    /* 120:8397 */ 0,
    /* 121:8398 */ 0,
    /* 122:8399 */ 0,
    /* 123:8417 */ 4,
    /* 124:8419 */ 4,
    /* 125:8424 */ 4,
    /* 126:8426 */ 4,
    /* 127:8428 */ 4,
    /* 128:8430 */ 4,
    /* 129:8482 */ 4,
    /* 130:8483 */ 0,
    /* 131:8485 */ 4,
    /* 132:8486 */ 4,
    /* 133:8487 */ 4,
    /* 134:8488 */ 4,
    /* 135:8489 */ 4,
    /* 136:8491 */ 0,
    /* 137:8493 */ 4,
    /* 138:9600 */ 4,
    /* 139:9604 */ 4,
    /* 140:9606 */ 4,
    /* 141:9607 */ 4,
    /* 142:9608 */ 4,
    /* 143:9609 */ 4,
    /* 144:10088 */ 4,
    /* 145:10089 */ 4,
    /* 146:10090 */ 4,
    /* 147:10091 */ 4,
    /* 148:10110 */ 0,
    /* 149:10111 */ 0,
    /* 150:10112 */ 4,
    /* 151:10158 */ 4,
    /* 152:10237 */ 4,
    /* 153:10240 */ 4,
    /* 154:10248 */ 4,
    /* 155:10249 */ 4,
    /* 156:10250 */ 4,
    /* 157:10251 */ 0,
    /* 158:10252 */ 4,
    /* 159:10593 */ 4,
    /* 160:10594 */ 4,
    /* 161:10599 */ 4,
    /* 162:10600 */ 4,
    /* 163:10635 */ 0,
    /* 164:10677 */ 4,
    /* 165:10684 */ 4,
    /* 166:10691 */ 4,
    /* 167:10702 */ 4,
    /* 168:10705 */ 4,
    /* 169:10731 */ 0,
    /* 170:10814 */ 4,
    /* 171:10816 */ 4,
    /* 172:10827 */ 4,
    /* 173:10829 */ 4,
    /* 174:10830 */ 4,
    /* 175:10831 */ 4,
    /* 176:10838 */ 4,
    /* 177:10853 */ 4,
    /* 178:10874 */ 4,
    /* 179:10875 */ 4,
    /* 180:10884 */ 4,
    /* 181:10886 */ 4,
    /* 182:10893 */ 4,
    /* 183:10894 */ 4,
    /* 184:10897 */ 4,
    /* 185:10899 */ 4,
    /* 186:10904 */ 4,
    /* 187:10910 */ 4,
    /* 188:10911 */ 0,
    /* 189:10918 */ 4,
    /* 190:10920 */ 4,
    /* 191:10933 */ 4,
    /* 192:10936 */ 4,
    /* 193:10937 */ 4,
    /* 194:10938 */ 4,
    /* 195:10949 */ 4,
    /* 196:10959 */ 4,
    /* 197:10965 */ 4,
    /* 198:10966 */ 4,
    /* 199:10967 */ 4,
    /* 200:10969 */ 4,
    /* 201:10970 */ 4,
    /* 202:10988 */ 4,
    /* 203:10990 */ 4,
    /* 204:11005 */ 4,
    /* 205:11027 */ 4,
    /* 206:11028 */ 4,
    /* 207:11053 */ 4,
    /* 208:11058 */ 4,
    /* 209:11089 */ 4,
    /* 210:11090 */ 4,
    /* 211:11732 */ 4,
    /* 212:11746 */ 4,
    /* 213:11833 */ 4,
    /* 214:11834 */ 4,
    /* 215:12056 */ 4,
    /* 216:12057 */ 4,
    /* 217:12064 */ 4,
    /* 218:12065 */ 4,
    /* 219:12066 */ 4,
    /* 220:12072 */ 4,
    /* 221:12143 */ 4,
    /* 222:12449 */ 4,
    /* 223:12450 */ 4,
    /* 224:13219 */ 0,
    /* 225:13223 */ 0,
    /* 226:13233 */ 0,
    /* 227:13293 */ 0,
    /* 228:13294 */ 0,
    /* 229:13295 */ 0,
    /* 230:13296 */ 0,
    /* 231:14177 */ 0,
    /* 232:14198 */ 0,
    /* 233:14200 */ 0,
    /* 234:14202 */ 0
};
