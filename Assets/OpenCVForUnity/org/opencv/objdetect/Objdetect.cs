﻿
//
// This file is auto-generated. Please don't modify it!
//
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace OpenCVForUnity
{
	public class Objdetect
	{

		public const int
			CASCADE_DO_CANNY_PRUNING = 1,
			CASCADE_SCALE_IMAGE = 2,
			CASCADE_FIND_BIGGEST_OBJECT = 4,
			CASCADE_DO_ROUGH_SEARCH = 8;
	
	
		//
		// C++:  void groupRectangles(vector_Rect& rectList, vector_int& weights, int groupThreshold, double eps = 0.2)
		//
	
		//javadoc: groupRectangles(rectList, weights, groupThreshold, eps)
		public static void groupRectangles (MatOfRect rectList, MatOfInt weights, int groupThreshold, double eps)
		{
			if (rectList != null)
				rectList.ThrowIfDisposed ();
			if (weights != null)
				weights.ThrowIfDisposed ();

#if UNITY_PRO_LICENSE || ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR) || UNITY_5
			Mat rectList_mat = rectList;
			Mat weights_mat = weights;
			objdetect_Objdetect_groupRectangles_10 (rectList_mat.nativeObj, weights_mat.nativeObj, groupThreshold, eps);
		
			return;
#else
return;
#endif
		}
	
		//javadoc: groupRectangles(rectList, weights, groupThreshold)
		public static void groupRectangles (MatOfRect rectList, MatOfInt weights, int groupThreshold)
		{
			if (rectList != null)
				rectList.ThrowIfDisposed ();
			if (weights != null)
				weights.ThrowIfDisposed ();

#if UNITY_PRO_LICENSE || ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR) || UNITY_5
			Mat rectList_mat = rectList;
			Mat weights_mat = weights;
			objdetect_Objdetect_groupRectangles_11 (rectList_mat.nativeObj, weights_mat.nativeObj, groupThreshold);
		
			return;
#else
return;
#endif
		}
	
	
		#if UNITY_IOS && !UNITY_EDITOR
		// C++:  void groupRectangles(vector_Rect& rectList, vector_int& weights, int groupThreshold, double eps = 0.2)
		[DllImport("__Internal")]
		private static extern void objdetect_Objdetect_groupRectangles_10 (IntPtr rectList_mat_nativeObj, IntPtr weights_mat_nativeObj, int groupThreshold, double eps);
		
		[DllImport("__Internal")]
		private static extern void objdetect_Objdetect_groupRectangles_11 (IntPtr rectList_mat_nativeObj, IntPtr weights_mat_nativeObj, int groupThreshold);
#else
	
		// C++:  void groupRectangles(vector_Rect& rectList, vector_int& weights, int groupThreshold, double eps = 0.2)
		[DllImport("opencvforunity")]
		private static extern void objdetect_Objdetect_groupRectangles_10 (IntPtr rectList_mat_nativeObj, IntPtr weights_mat_nativeObj, int groupThreshold, double eps);

		[DllImport("opencvforunity")]
		private static extern void objdetect_Objdetect_groupRectangles_11 (IntPtr rectList_mat_nativeObj, IntPtr weights_mat_nativeObj, int groupThreshold);
#endif
	
	}
}
