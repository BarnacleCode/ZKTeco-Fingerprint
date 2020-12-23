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

*"----------------------------------------------------------------------
*"*"Local Interface:
*"  IMPORTING
*"     VALUE(MODE) TYPE  CHAR1
*"     VALUE(FINGERPRINT) TYPE  ZDE_HASH OPTIONAL
*"     VALUE(INDX) TYPE  I OPTIONAL
*"     VALUE(NAMA) TYPE  CHAR50 OPTIONAL
*"  CHANGING
*"     VALUE(EX_FINGERPRINT) TYPE  ZTBT_FINGER
*"----------------------------------------------------------------------
  DATA : lv_idnumber TYPE guid_16.
  DATA:
    gs_store_file TYPE ztable_finger,
    gt_content    TYPE STANDARD TABLE OF string,
    xstr_content  TYPE xstring.

  DATA : li_tt TYPE TABLE OF ztable_finger WITH HEADER LINE.

  CASE mode.

    WHEN 'R'.
      CLEAR gs_store_file.

      SELECT * FROM ztable_finger INTO TABLE li_tt.

      SORT li_tt BY idnumber DESCENDING.

      CLEAR li_tt.
      READ TABLE li_tt INDEX 1.

      gs_store_file-idnumber    = li_tt-idnumber + 1.
      gs_store_file-fingerprint = fingerprint.
      gs_store_file-nama        = nama.


      MODIFY ztable_finger FROM gs_store_file.
      COMMIT WORK.

    WHEN 'S'.
      SELECT fingerprint FROM ztable_finger INTO TABLE ex_fingerprint.

    WHEN 'L'.
      SELECT * FROM ztable_finger INTO TABLE li_tt.

      READ TABLE li_tt INDEX indx.

      li_tt-cpudt = sy-datum.
      li_tt-cputm = sy-uzeit.

      MODIFY ztable_finger FROM li_tt.
      COMMIT WORK.
  ENDCASE.

