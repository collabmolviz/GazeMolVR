using UnityEngine;
using System.Collections;
using System;
using System.Runtime.InteropServices;

namespace UMol {
public class MDDriverWrapper {

    [DllImport ("Unity_MDDriver")]
    public static extern IntPtr createMDDriverInstance();

    [DllImport ("Unity_MDDriver")]
    public static extern void deleteMDDriverInstance(IntPtr instance);

    [DllImport ("Unity_MDDriver")]
    public static extern void MDDriver_init(IntPtr instance, string hostname, int port);

    [DllImport ("Unity_MDDriver")]
    public static extern int MDDriver_start(IntPtr instance);

    [DllImport ("Unity_MDDriver")]
    public static extern int MDDriver_stop(IntPtr instance);

    [DllImport ("Unity_MDDriver")]
    public static extern bool MDDriver_isConnected(IntPtr instance);
    // public static extern int MDDriver_probeconnection();

    [DllImport ("Unity_MDDriver")]
    public static extern int MDDriver_getNbParticles(IntPtr instance);

    [DllImport ("Unity_MDDriver")]
    public static extern int MDDriver_getPositions(IntPtr instance, [In, Out] float[] verts, int nbParticles);

    [DllImport ("Unity_MDDriver")]
    public static extern void MDDriver_pause(IntPtr instance);

    [DllImport ("Unity_MDDriver")]
    public static extern void MDDriver_play(IntPtr instance);

    [DllImport ("Unity_MDDriver")]
    public static extern void MDDriver_setForces(IntPtr instance, int nbforces, int[] atomslist, float[] forceslist);

    [DllImport ("Unity_MDDriver")]
    public static extern void MDDriver_getEnergies(IntPtr instance, ref IMDEnergies energies);

    [DllImport ("Unity_MDDriver")]
    public static extern int MDDriver_loop(IntPtr instance);

    [DllImport ("Unity_MDDriver")]
    public static extern void MDDriver_disconnect(IntPtr instance);
    

    public struct IMDEnergies
    {
        public int tstep;  //!< integer timestep index
        public float T;          //!< Temperature in degrees Kelvin
        public float Etot;       //!< Total energy, in Kcal/mol
        public float Epot;       //!< Potential energy, in Kcal/mol
        public float Evdw;       //!< Van der Waals energy, in Kcal/mol
        public float Eelec;      //!< Electrostatic energy, in Kcal/mol
        public float Ebond;      //!< Bond energy, Kcal/mol
        public float Eangle;     //!< Angle energy, Kcal/mol
        public float Edihe;      //!< Dihedral energy, Kcal/mol
        public float Eimpr;
    };

}
}