﻿namespace NeoEdit.Common
{
	public enum NECommand
	{
		None,
		[NoMacro] Internal_CommandLine,
		[NoMacro] Internal_Activate,
		[NoMacro] Internal_CloseFile,
		Internal_Key,
		Internal_Text,
		Internal_SetBinaryValue,
		[NoMacro] Internal_Scroll,
		[NoMacro] Internal_Mouse,
		[NoMacro] Internal_Redraw,
		File_Select_All,
		File_Select_None,
		File_Select_WithSelections,
		File_Select_WithoutSelections,
		File_Select_Modified,
		File_Select_Unmodified,
		File_Select_ExternalModified,
		File_Select_ExternalUnmodified,
		File_Select_Inactive,
		File_Select_SelectByExpression,
		[NoMacro] File_Select_Choose,
		File_New_New,
		File_New_FromSelections_All,
		File_New_FromSelections_Files,
		File_New_FromSelections_Selections,
		File_New_FromClipboard_All,
		File_New_FromClipboard_Files,
		File_New_FromClipboard_Selections,
		File_New_WordList,
		File_Open_Open,
		File_Open_CopiedCut,
		File_Open_ReopenWithEncoding,
		File_Open_Revert,
		File_Open_Refresh,
		File_Open_AutoRefresh,
		File_Save_SaveModified,
		File_Save_SaveAll,
		File_Save_SaveAs,
		File_Save_SaveAsByExpression,
		File_Move_Move,
		File_Move_MoveByExpression,
		File_Copy_Copy,
		File_Copy_CopyByExpression,
		File_Copy_Path,
		File_Copy_Name,
		File_Copy_DisplayName,
		File_Delete,
		File_Encoding,
		File_LineEndings,
		File_FileIndex,
		File_ActiveFileIndex,
		File_Advanced_Compress,
		File_Advanced_Encrypt,
		File_Advanced_Explore,
		File_Advanced_CommandPrompt,
		File_Advanced_DragDrop,
		File_Advanced_SetDisplayName,
		File_Advanced_DontExitOnClose,
		File_Close_Active,
		File_Close_Inactive,
		File_Close_WithSelections,
		File_Close_WithoutSelections,
		File_Close_Modified,
		File_Close_Unmodified,
		File_Close_ExternalModified,
		File_Close_ExternalUnmodified,
		File_Exit,
		Edit_Select_All,
		Edit_Select_Nothing,
		Edit_Select_Join,
		Edit_Select_Invert,
		Edit_Select_Limit,
		Edit_Select_Lines,
		Edit_Select_WholeLines,
		Edit_Select_Empty,
		Edit_Select_NonEmpty,
		Edit_Select_AllowOverlappingSelections,
		Edit_Select_ToggleAnchor,
		Edit_Select_Focused_First,
		Edit_Select_Focused_Next,
		Edit_Select_Focused_Previous,
		Edit_Select_Focused_Single,
		Edit_Select_Focused_Remove,
		Edit_Select_Focused_RemoveBefore,
		Edit_Select_Focused_RemoveAfter,
		Edit_Select_Focused_CenterVertically,
		Edit_Select_Focused_Center,
		Edit_Copy,
		Edit_Cut,
		Edit_Paste_Paste,
		Edit_Paste_RotatePaste,
		Edit_Undo_Text,
		Edit_Undo_Step,
		Edit_Undo_Global,
		Edit_Redo_Text,
		Edit_Redo_Step,
		Edit_Repeat,
		Edit_Rotate,
		Edit_Expression_Expression,
		Edit_Expression_EvaluateSelected,
		Edit_ModifyRegions,
		Edit_Navigate_WordLeft,
		Edit_Navigate_WordRight,
		Edit_Navigate_AllLeft,
		Edit_Navigate_AllRight,
		Edit_Navigate_JumpBy_Words,
		Edit_Navigate_JumpBy_Numbers,
		Edit_Navigate_JumpBy_Paths,
		Edit_RepeatCount,
		Edit_RepeatIndex,
		Edit_Advanced_Convert,
		Edit_Advanced_Hash,
		Edit_Advanced_Compress,
		Edit_Advanced_Decompress,
		Edit_Advanced_Encrypt,
		Edit_Advanced_Decrypt,
		Edit_Advanced_Sign,
		Edit_Advanced_RunCommand_Parallel,
		Edit_Advanced_RunCommand_Sequential,
		Edit_Advanced_RunCommand_Shell,
		Text_Select_WholeWord,
		Text_Select_BoundedWord,
		Text_Select_Trim,
		Text_Select_Split,
		Text_Select_Repeats_Unique_IgnoreCase,
		Text_Select_Repeats_Unique_MatchCase,
		Text_Select_Repeats_Duplicates_IgnoreCase,
		Text_Select_Repeats_Duplicates_MatchCase,
		Text_Select_Repeats_NonMatchPrevious_IgnoreCase,
		Text_Select_Repeats_NonMatchPrevious_MatchCase,
		Text_Select_Repeats_MatchPrevious_IgnoreCase,
		Text_Select_Repeats_MatchPrevious_MatchCase,
		Text_Select_Repeats_ByCount_IgnoreCase,
		Text_Select_Repeats_ByCount_MatchCase,
		Text_Select_Repeats_BetweenFiles_Matches_Ordered_IgnoreCase,
		Text_Select_Repeats_BetweenFiles_Matches_Ordered_MatchCase,
		Text_Select_Repeats_BetweenFiles_Matches_Unordered_IgnoreCase,
		Text_Select_Repeats_BetweenFiles_Matches_Unordered_MatchCase,
		Text_Select_Repeats_BetweenFiles_Diffs_Ordered_IgnoreCase,
		Text_Select_Repeats_BetweenFiles_Diffs_Ordered_MatchCase,
		Text_Select_Repeats_BetweenFiles_Diffs_Unordered_IgnoreCase,
		Text_Select_Repeats_BetweenFiles_Diffs_Unordered_MatchCase,
		Text_Select_ByWidth,
		Text_Select_Min_Text,
		Text_Select_Min_Length,
		Text_Select_Max_Text,
		Text_Select_Max_Length,
		Text_Select_ToggleOpenClose,
		Text_Find_Find,
		Text_Find_RegexReplace,
		Text_Trim,
		Text_Width,
		Text_SingleLine,
		Text_Case_Upper,
		Text_Case_Lower,
		Text_Case_Proper,
		Text_Case_Invert,
		Text_Sort,
		Text_Escape_Markup,
		Text_Escape_Regex,
		Text_Escape_URL,
		Text_Unescape_Markup,
		Text_Unescape_Regex,
		Text_Unescape_URL,
		Text_Transform,
		Text_Random,
		Text_Advanced_Unicode,
		Text_Advanced_FirstDistinct,
		Text_Advanced_GUID,
		Text_Advanced_ReverseRegex,
		Numeric_Select_Min,
		Numeric_Select_Max,
		Numeric_Select_Limit,
		Numeric_Round,
		Numeric_Floor,
		Numeric_Ceiling,
		Numeric_Sum_Sum,
		Numeric_Sum_Increment,
		Numeric_Sum_Decrement,
		Numeric_Sum_AddClipboard,
		Numeric_Sum_SubtractClipboard,
		Numeric_Sum_ForwardSum,
		Numeric_Sum_UndoForwardSum,
		Numeric_Sum_ReverseSum,
		Numeric_Sum_UndoReverseSum,
		Numeric_AbsoluteValue,
		Numeric_Scale,
		Numeric_Cycle,
		Numeric_Trim,
		Numeric_Fraction,
		Numeric_Factor,
		Numeric_Series_ZeroBased,
		Numeric_Series_OneBased,
		Numeric_Series_Linear,
		Numeric_Series_Geometric,
		Numeric_ConvertBase_ToHex,
		Numeric_ConvertBase_FromHex,
		Numeric_ConvertBase_ConvertBase,
		Numeric_RandomNumber,
		Numeric_CombinationsPermutations,
		Numeric_MinMaxValues,
		Files_Select_Files,
		Files_Select_Directories,
		Files_Select_Existing,
		Files_Select_NonExisting,
		Files_Select_Name_Directory,
		Files_Select_Name_Name,
		Files_Select_Name_NameWOExtension,
		Files_Select_Name_Extension,
		Files_Select_Name_Next,
		Files_Select_Name_CommonAncestor,
		Files_Select_Name_MatchDepth,
		Files_Select_Roots,
		Files_Select_NonRoots,
		Files_Select_ByContent,
		Files_Select_BySourceControlStatus,
		Files_Copy,
		Files_Move,
		Files_Delete,
		Files_Name_MakeAbsolute,
		Files_Name_MakeRelative,
		Files_Name_Simplify,
		Files_Name_Sanitize,
		Files_Get_Size,
		Files_Get_Time_Write,
		Files_Get_Time_Access,
		Files_Get_Time_Create,
		Files_Get_Attributes,
		Files_Get_Version_File,
		Files_Get_Version_Product,
		Files_Get_Version_Assembly,
		Files_Get_Hash,
		Files_Get_SourceControlStatus,
		Files_Get_Children,
		Files_Get_Descendants,
		Files_Get_Content,
		Files_Set_Size,
		Files_Set_Time_Write,
		Files_Set_Time_Access,
		Files_Set_Time_Create,
		Files_Set_Time_All,
		Files_Set_Attributes,
		Files_Set_Content,
		Files_Set_Encoding,
		Files_Create_Files,
		Files_Create_Directories,
		Files_Compress,
		Files_Decompress,
		Files_Encrypt,
		Files_Decrypt,
		Files_Sign,
		Files_Advanced_Explore,
		Files_Advanced_CommandPrompt,
		Files_Advanced_DragDrop,
		Files_Advanced_SplitFiles,
		Files_Advanced_CombineFiles,
		Files_Advanced_ExtractPDF,
		Content_Type_SetFromExtension,
		Content_Type_None,
		Content_Type_Balanced,
		Content_Type_Columns,
		Content_Type_CPlusPlus,
		Content_Type_CSharp,
		Content_Type_CSV,
		Content_Type_ExactColumns,
		Content_Type_HTML,
		Content_Type_JSON,
		Content_Type_SQL,
		Content_Type_TSV,
		Content_Type_XML,
		Content_HighlightSyntax,
		Content_StrictParsing,
		Content_Reformat,
		Content_Comment,
		Content_Uncomment,
		Content_Copy,
		Content_TogglePosition,
		Content_Current,
		Content_Parent,
		Content_Ancestor,
		Content_Attributes,
		Content_WithAttribute,
		Content_Children_Children,
		Content_Children_SelfAndChildren,
		Content_Children_First,
		Content_Children_WithAttribute,
		Content_Descendants_Descendants,
		Content_Descendants_SelfAndDescendants,
		Content_Descendants_First,
		Content_Descendants_WithAttribute,
		Content_Navigate_Up,
		Content_Navigate_Down,
		Content_Navigate_Left,
		Content_Navigate_Right,
		Content_Navigate_Home,
		Content_Navigate_End,
		Content_Navigate_Pgup,
		Content_Navigate_Pgdn,
		Content_Navigate_Row,
		Content_Navigate_Column,
		Content_KeepSelections,
		DateTime_Now,
		DateTime_UTCNow,
		DateTime_ToUTC,
		DateTime_ToLocal,
		DateTime_ToTimeZone,
		DateTime_Format,
		DateTime_AddClipboard,
		DateTime_SubtractClipboard,
		Table_Select_RowsByExpression,
		Table_New_FromSelection,
		Table_New_FromLineSelections,
		Table_New_FromRegionSelections_Region1,
		Table_New_FromRegionSelections_Region2,
		Table_New_FromRegionSelections_Region3,
		Table_New_FromRegionSelections_Region4,
		Table_New_FromRegionSelections_Region5,
		Table_New_FromRegionSelections_Region6,
		Table_New_FromRegionSelections_Region7,
		Table_New_FromRegionSelections_Region8,
		Table_New_FromRegionSelections_Region9,
		Table_Edit,
		Table_DetectType,
		Table_Convert,
		Table_SetJoinSource,
		Table_Join,
		Table_Transpose,
		Table_Database_GenerateInserts,
		Table_Database_GenerateUpdates,
		Table_Database_GenerateDeletes,
		Image_Size,
		Image_Crop,
		Image_GrabColor,
		Image_GrabImage,
		Image_AddColor,
		Image_AdjustColor,
		Image_OverlayColor,
		Image_FlipHorizontal,
		Image_FlipVertical,
		Image_Rotate,
		Image_GIF_Animate,
		Image_GIF_Split,
		Image_GetTakenDate,
		Image_SetTakenDate,
		Position_Goto_Lines,
		Position_Goto_Columns,
		Position_Goto_Indexes,
		Position_Goto_Positions,
		Position_Copy_Lines,
		Position_Copy_Columns,
		Position_Copy_Indexes,
		Position_Copy_Positions,
		Diff_Select_Matches,
		Diff_Select_Diffs,
		Diff_Select_LeftFile,
		Diff_Select_RightFile,
		Diff_Select_BothFiles,
		Diff_Diff,
		Diff_Break,
		Diff_SourceControl,
		Diff_IgnoreWhitespace,
		Diff_IgnoreCase,
		Diff_IgnoreNumbers,
		Diff_IgnoreLineEndings,
		Diff_IgnoreCharacters,
		Diff_Reset,
		Diff_Next,
		Diff_Previous,
		Diff_CopyLeft,
		Diff_CopyRight,
		Diff_Fix_Whitespace,
		Diff_Fix_Case,
		Diff_Fix_Numbers,
		Diff_Fix_LineEndings,
		Diff_Fix_Encoding,
		Network_AbsoluteURL,
		Network_Fetch_Fetch,
		Network_Fetch_Hex,
		Network_Fetch_File,
		Network_Fetch_Custom,
		Network_Fetch_Certificate,
		Network_Fetch_Stream,
		Network_Fetch_Playlist,
		Network_Lookup_IP,
		Network_Lookup_Hostname,
		Network_AdaptersInfo,
		Network_Ping,
		Network_ScanPorts,
		Network_WCF_GetConfig,
		Network_WCF_Execute,
		Network_WCF_InterceptCalls,
		Network_WCF_ResetClients,
		Database_Connect,
		Database_TestConnection,
		Database_ExecuteQuery,
		Database_Examine,
		Database_GetSproc,
		KeyValue_Set_Keys_IgnoreCase,
		KeyValue_Set_Keys_MatchCase,
		KeyValue_Set_Values1,
		KeyValue_Set_Values2,
		KeyValue_Set_Values3,
		KeyValue_Set_Values4,
		KeyValue_Set_Values5,
		KeyValue_Set_Values6,
		KeyValue_Set_Values7,
		KeyValue_Set_Values8,
		KeyValue_Set_Values9,
		KeyValue_Add_Keys,
		KeyValue_Add_Values1,
		KeyValue_Add_Values2,
		KeyValue_Add_Values3,
		KeyValue_Add_Values4,
		KeyValue_Add_Values5,
		KeyValue_Add_Values6,
		KeyValue_Add_Values7,
		KeyValue_Add_Values8,
		KeyValue_Add_Values9,
		KeyValue_Remove_Keys,
		KeyValue_Remove_Values1,
		KeyValue_Remove_Values2,
		KeyValue_Remove_Values3,
		KeyValue_Remove_Values4,
		KeyValue_Remove_Values5,
		KeyValue_Remove_Values6,
		KeyValue_Remove_Values7,
		KeyValue_Remove_Values8,
		KeyValue_Remove_Values9,
		KeyValue_Replace_Values1,
		KeyValue_Replace_Values2,
		KeyValue_Replace_Values3,
		KeyValue_Replace_Values4,
		KeyValue_Replace_Values5,
		KeyValue_Replace_Values6,
		KeyValue_Replace_Values7,
		KeyValue_Replace_Values8,
		KeyValue_Replace_Values9,
		[NoMacro] Macro_Play_Quick_1,
		[NoMacro] Macro_Play_Quick_2,
		[NoMacro] Macro_Play_Quick_3,
		[NoMacro] Macro_Play_Quick_4,
		[NoMacro] Macro_Play_Quick_5,
		[NoMacro] Macro_Play_Quick_6,
		[NoMacro] Macro_Play_Quick_7,
		[NoMacro] Macro_Play_Quick_8,
		[NoMacro] Macro_Play_Quick_9,
		[NoMacro] Macro_Play_Quick_10,
		[NoMacro] Macro_Play_Quick_11,
		[NoMacro] Macro_Play_Quick_12,
		[NoMacro] Macro_Play_Play,
		[NoMacro] Macro_Play_Repeat,
		[NoMacro] Macro_Play_PlayOnCopiedFiles,
		[NoMacro] Macro_Record_Quick_1,
		[NoMacro] Macro_Record_Quick_2,
		[NoMacro] Macro_Record_Quick_3,
		[NoMacro] Macro_Record_Quick_4,
		[NoMacro] Macro_Record_Quick_5,
		[NoMacro] Macro_Record_Quick_6,
		[NoMacro] Macro_Record_Quick_7,
		[NoMacro] Macro_Record_Quick_8,
		[NoMacro] Macro_Record_Quick_9,
		[NoMacro] Macro_Record_Quick_10,
		[NoMacro] Macro_Record_Quick_11,
		[NoMacro] Macro_Record_Quick_12,
		[NoMacro] Macro_Record_Record,
		[NoMacro] Macro_Record_StopRecording,
		[NoMacro] Macro_Append_Quick_1,
		[NoMacro] Macro_Append_Quick_2,
		[NoMacro] Macro_Append_Quick_3,
		[NoMacro] Macro_Append_Quick_4,
		[NoMacro] Macro_Append_Quick_5,
		[NoMacro] Macro_Append_Quick_6,
		[NoMacro] Macro_Append_Quick_7,
		[NoMacro] Macro_Append_Quick_8,
		[NoMacro] Macro_Append_Quick_9,
		[NoMacro] Macro_Append_Quick_10,
		[NoMacro] Macro_Append_Quick_11,
		[NoMacro] Macro_Append_Quick_12,
		[NoMacro] Macro_Append_Append,
		Macro_Open_Quick_1,
		Macro_Open_Quick_2,
		Macro_Open_Quick_3,
		Macro_Open_Quick_4,
		Macro_Open_Quick_5,
		Macro_Open_Quick_6,
		Macro_Open_Quick_7,
		Macro_Open_Quick_8,
		Macro_Open_Quick_9,
		Macro_Open_Quick_10,
		Macro_Open_Quick_11,
		Macro_Open_Quick_12,
		Macro_Open_Open,
		Macro_RepeatLastAction,
		Macro_Visualize,
		Window_New_New,
		Window_New_FromSelections_All,
		Window_New_FromSelections_Files,
		Window_New_FromSelections_Selections,
		Window_New_SummarizeSelections_Files_IgnoreCase,
		Window_New_SummarizeSelections_Files_MatchCase,
		Window_New_SummarizeSelections_Selections_IgnoreCase,
		Window_New_SummarizeSelections_Selections_MatchCase,
		Window_New_FromClipboard_All,
		Window_New_FromClipboard_Files,
		Window_New_FromClipboard_Selections,
		Window_New_FromFiles_Active,
		Window_New_FromFiles_CopiedCut,
		Window_Full,
		Window_Grid,
		Window_CustomGrid,
		Window_WorkMode,
		Window_Font_Size,
		Window_Font_ShowSpecial,
		Window_ViewBinary,
		Window_BinaryCodePages,
		Help_Tutorial,
		Help_Update,
		Help_Advanced_TimeNextAction,
		Help_Advanced_Shell_Integrate,
		Help_Advanced_Shell_Unintegrate,
		Help_Advanced_CopyCommandLine,
		Help_Advanced_RunGC,
		Help_About,
	}
}
