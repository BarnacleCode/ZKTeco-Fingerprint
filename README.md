#
This program aims to connect ZKTeco Fingerprint with SAP.

Prerequisite :

1.Install SAP .Net Connector 4 x86

2.Install ZKFingerSDK+5.3_ZK10.0

3.Install ZKFinger Driver

Login into your SAP.

1.Create one Table ZTABLE_FINGER

IDNUMBER	  Type  INT1	
FINGERPRINT	Type  STRING
NAMA	      Type  CHar 30
CPUDT	      Type  DATS
CPUTM	      Type  TIMS

2.Create RFC ZFM_TABLE_FINGER


