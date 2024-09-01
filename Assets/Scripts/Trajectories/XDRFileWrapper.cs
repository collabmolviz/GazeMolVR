using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

// namespace Trajectories {
	public enum XDRStatus 
		{ exdrOK, exdrHEADER, exdrSTRING, exdrDOUBLE, 
		exdrINT, exdrFLOAT, exdrUINT, exdr3DX, exdrCLOSE, exdrMAGIC,
		exdrNOMEM, exdrENDOFFILE, exdrFILENOTFOUND, exdrNR };

	// Provides function prototypes to use the xdrfile library.
	public class XDRFileWrapper {
		// Opens a trajectory file located at "path" using the provided mode.
		// mode = "r" for read, mode = "w" for write.
		// Returns a file pointer to an xdr file datatype, or NULL if an error occurs.
		[DllImport ("xdrfile", CallingConvention=CallingConvention.Cdecl)]
		public static extern System.IntPtr xdrfile_open([In] string path, [In] string mode);

		// Closes a previously opened trajectory file passed in argument.
		// Returns 0 on success (XDRStatus.endrOK), non-zero on error.
		[DllImport ("xdrfile", CallingConvention=CallingConvention.Cdecl)]
		public static extern XDRStatus xdrfile_close([In] System.IntPtr xfp);

		// Returns the number of atoms in the xtc file into *natoms.
		// Returns 0 on success (XDRStatus.endrOK), non-zero on error.
		[DllImport ("xdrfile", CallingConvention=CallingConvention.Cdecl)]
		public static extern XDRStatus read_xtc_natoms([In] string filename, ref int natoms);

		// Reads one frame of an opened xtc file.
		// Returns 0 on success (XDRStatus.endrOK), non-zero on error.
		[DllImport ("xdrfile", CallingConvention=CallingConvention.Cdecl)]
		public static extern XDRStatus read_xtc(System.IntPtr xd, int natoms, ref int step, ref float time, float[,] box, [In, Out] float[] x, ref float prec);

		// Returns the number of atoms in the xtc file into *natoms.
		// Returns 0 on success (XDRStatus.endrOK), non-zero on error.
		[DllImport ("xdrfile", CallingConvention=CallingConvention.Cdecl)]
		public static extern XDRStatus read_trr_natoms([In] string filename, ref int natoms);

		// Reads one frame of an opened trr file.
		// Returns 0 on success (XDRStatus.endrOK), non-zero on error.
		[DllImport ("xdrfile", CallingConvention=CallingConvention.Cdecl)]
		public static extern XDRStatus read_trr(System.IntPtr xd, int natoms, ref int step, ref float time, ref float lambda, float[,] box, [In, Out] float[] x, [In, Out] float[] v, [In, Out] float[] f);

		[DllImport ("xdrfile", CallingConvention=CallingConvention.Cdecl)]
		public static extern int read_xtc_numframes([In] string filename, ref int natoms, ref System.IntPtr offsets);

		[DllImport ("xdrfile", CallingConvention=CallingConvention.Cdecl)]
		public static extern int read_trr_numframes([In] string filename, ref int natoms, ref System.IntPtr offsets);

		[DllImport ("xdrfile", CallingConvention=CallingConvention.Cdecl)]
		public static extern XDRStatus xdr_seek(System.IntPtr xd, long pos, int whence);

		[DllImport ("xdrfile", CallingConvention=CallingConvention.Cdecl)]
		public static extern long xdr_tell(System.IntPtr xd);
	}
// }