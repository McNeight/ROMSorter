﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RomSorter
{
    class DTDReference
    {
        //The formantits 
//        <!--
//   ROM Management Datafile - DTD

//   For further information, see: http://www.logiqx.com/

//   This DTD module is identified by the PUBLIC and SYSTEM identifiers:

//   PUBLIC "-//Logiqx//DTD ROM Management Datafile//EN"
//   SYSTEM "http://www.logiqx.com/Dats/datafile.dtd"

//   $Revision: 1.5 $
//   $Date: 2008/10/28 21:39:16 $

//-->

//<!ELEMENT datafile(header?, game+)>
//	<!ATTLIST datafile build CDATA #IMPLIED>
//	<!ATTLIST datafile debug (yes|no) "no">
//	<!ELEMENT header(name, description, category?, version, date?, author, email?, homepage?, url?, comment?, clrmamepro?, romcenter?)>
//		<!ELEMENT name(#PCDATA)>
//		<!ELEMENT description (#PCDATA)>
//		<!ELEMENT category (#PCDATA)>
//		<!ELEMENT version (#PCDATA)>
//		<!ELEMENT date (#PCDATA)>
//		<!ELEMENT author (#PCDATA)>
//		<!ELEMENT email (#PCDATA)>
//		<!ELEMENT homepage (#PCDATA)>
//		<!ELEMENT url (#PCDATA)>
//		<!ELEMENT comment (#PCDATA)>
//		<!ELEMENT clrmamepro EMPTY>
//			<!ATTLIST clrmamepro header CDATA #IMPLIED>
//			<!ATTLIST clrmamepro forcemerging (none|split|full) "split">
//			<!ATTLIST clrmamepro forcenodump(obsolete|required|ignore) "obsolete">
//			<!ATTLIST clrmamepro forcepacking(zip|unzip) "zip">
//		<!ELEMENT romcenter EMPTY>
//			<!ATTLIST romcenter plugin CDATA #IMPLIED>
//			<!ATTLIST romcenter rommode (merged|split|unmerged) "split">
//			<!ATTLIST romcenter biosmode(merged|split|unmerged) "split">
//			<!ATTLIST romcenter samplemode(merged|unmerged) "merged">
//			<!ATTLIST romcenter lockrommode(yes|no) "no">
//			<!ATTLIST romcenter lockbiosmode(yes|no) "no">
//			<!ATTLIST romcenter locksamplemode(yes|no) "no">
//	<!ELEMENT game(comment*, description, year?, manufacturer?, release*, biosset*, rom*, disk*, sample*, archive*)>
//		<!ATTLIST game name CDATA #REQUIRED>
//		<!ATTLIST game sourcefile CDATA #IMPLIED>
//		<!ATTLIST game isbios (yes|no) "no">
//		<!ATTLIST game cloneof CDATA #IMPLIED>
//		<!ATTLIST game romof CDATA #IMPLIED>
//		<!ATTLIST game sampleof CDATA #IMPLIED>
//		<!ATTLIST game board CDATA #IMPLIED>
//		<!ATTLIST game rebuildto CDATA #IMPLIED>
//		<!ELEMENT year (#PCDATA)>
//		<!ELEMENT manufacturer (#PCDATA)>
//		<!ELEMENT release EMPTY>
//			<!ATTLIST release name CDATA #REQUIRED>
//			<!ATTLIST release region CDATA #REQUIRED>
//			<!ATTLIST release language CDATA #IMPLIED>
//			<!ATTLIST release date CDATA #IMPLIED>
//			<!ATTLIST release default (yes|no) "no">	
//		<!ELEMENT biosset EMPTY>
//			<!ATTLIST biosset name CDATA #REQUIRED>
//			<!ATTLIST biosset description CDATA #REQUIRED>
//			<!ATTLIST biosset default (yes|no) "no">
//		<!ELEMENT rom EMPTY>
//			<!ATTLIST rom name CDATA #REQUIRED>
//			<!ATTLIST rom size CDATA #REQUIRED>
//			<!ATTLIST rom crc CDATA #IMPLIED>
//			<!ATTLIST rom sha1 CDATA #IMPLIED>
//			<!ATTLIST rom md5 CDATA #IMPLIED>
//			<!ATTLIST rom merge CDATA #IMPLIED>
//			<!ATTLIST rom status (baddump|nodump|good|verified) "good">
//			<!ATTLIST rom date CDATA #IMPLIED>
//		<!ELEMENT disk EMPTY>
//			<!ATTLIST disk name CDATA #REQUIRED>
//			<!ATTLIST disk sha1 CDATA #IMPLIED>
//			<!ATTLIST disk md5 CDATA #IMPLIED>
//			<!ATTLIST disk merge CDATA #IMPLIED>
//			<!ATTLIST disk status (baddump|nodump|good|verified) "good">
//		<!ELEMENT sample EMPTY>
//			<!ATTLIST sample name CDATA #REQUIRED>
//		<!ELEMENT archive EMPTY>
//			<!ATTLIST archive name CDATA #REQUIRED>



    }
}
