namespace AElf.TokenSwap
{
    public class SqlStatementHelper
    {
        public static string GetCheckSql(string name, string idCard, string year)
        {
            return $@"
SELECT
CASE
 WHEN count(1) > 0 AND max(t.zczt)<3 THEN 4 /**资产不符合要求**/
 WHEN count(1) > 0 AND sum(t.zchmj) = 0 THEN 3 /**资产不存在**/
 WHEN count(1)= 0 THEN 2 /**用户不存在**/
 WHEN count(1) > 0 THEN 1 /**核验通过**/
END result
FROM
 entity_tdbchmc t
WHERE
  t.sfsc = 2
 AND t.skr = '{name}' 
 AND t.sfzh = '{idCard}'
" +
                   (string.IsNullOrEmpty(year)
                       ? ""
                       : $"AND (CAST(LEFT( t.cjsj, 4) as SIGNED) = {year} or CAST(LEFT( t.cjsj, 4) as SIGNED)+1 = {year})"
                   );
        }

        public static string GetConstructionCheckSql(string name, string idCard, string year)
        {
            return $@"
SELECT 
CASE WHEN count(1) > 0 AND max(a.zczt)< 3 THEN 4 /**资产不符合要求**/ 
    WHEN count(1) > 0 AND sum(CASE WHEN a.bfzt = 4 THEN 1 ELSE 0 END) = 0 THEN 3 /**资产不存在**/		
		WHEN count(1)= 0 THEN 2 /**用户不存在**/		
		WHEN count(1) > 0 THEN 1 /**核验通过**/		
	END result 
FROM
	cet_zj_lwrygz_lwry a
	LEFT JOIN cet_zj_lwrygz b ON a.cet_zj_lwrygz_id = b.id 
WHERE
  a.xm = '{name}' 
 AND a.sfzhm = '{idCard}' 
" +
                   (string.IsNullOrEmpty(year)
                       ? ""
                       : $"AND CAST( LEFT ( b.casj, 4 ) AS SIGNED ) = {year}"
                   );
            
        }

        public static string GetQueryCreditSql(string name, string idCard)
        {
            return $@"
select a.skr as name,a.sfzh as idcard, 1 as asset_type,a.id as asset_id,concat_ws('-',a.szzw,a.id) as blockId,a.id,a.skr,a.sfzh,b.yhlx as yhlx,c.yhmc as khszyh,a.khyh,a.lhh,a.yhzh,case a.sfkh when 1 then '同行' when 2 then '跨行' end as sfkh,case a.zhxz when 1 then '对私账号' when 2 then '对公账号' end as zhxz,a.ntfw,a.dz,a.xz,a.nz,a.bf,a.zchmj,a.dtzw,a.sc,a.jjzw,a.sm,a.smz,a.dpzw,a.qt,a.tdsyj,a.dtzwje,a.scje,a.qtzwje,a.dpzwje,a.qtfzwje,a.bczje,a.zjedx,a.fj,a.bz,a.cjsj,a.cjr,case a.bfzt when 1 then '拨付中' when 2 then '拨付成功' when 3 then '拨付失败' when 4 then '未发放' when 5 then '撤销成功' end as bfzt,case a.sfsc when 1 then '已删除' when 2 then '未删除' end sfsc,a.htfj,a.tdbcmxpc,a.dealno,d.xmmc,a.szzw,a.lsx,case a.sfxx when 1 then '村委会' when 2 then '村民' end as sfxx
 from entity_tdbchmc a left join entity_yxlx b on a.yhlx=b.id  /**关联银行**/
 left join entity_yxxxgl c on a.khszyh = c.id    /**关联开户行**/
 left join entity_xmxxgl d on a.xmmc = d.id      /**关联项目信息**/
 left join entity_bcmxsc e on a.tdbcmxpc = e.id  /**关联补偿批次信息**/
	where a.skr = '{name}' and a.sfzh = '{idCard}' 
";
        }
        
        public static string GetQueryConstructionCreditSql(string name, string idCard)
        {
            return $@"
SELECT 
  a.id as asset_id,a.xm as name,a.sfzhm as idcard,a.sfje as bczje
FROM
	cet_zj_lwrygz_lwry a
	LEFT JOIN cet_zj_lwrygz b ON a.cet_zj_lwrygz_id = b.id 
WHERE
     a.bfzt = 4
	and a.xm = '{name}'                       #姓名
	AND a.sfzhm = '{idCard}'       #身份证号
";
        }
        
        public static string GetChangeStatusSql(string name, string idCard, string assetId, string status)
        {
            return $@"
UPDATE entity_tdbchmc t 
SET t.zczt = {status}       #更新资产状态
WHERE
	t.id = {assetId}        #资产ID	
	AND t.skr = '{name}'  #姓名	
	AND t.sfzh = '{idCard}' #身份证号
";
        }
        
        public static string GetChangeStatusForConstructionSql(string name, string idCard, string assetId, string status)
        {
            return $@"
UPDATE cet_zj_lwrygz_lwry t 
SET t.zczt = {status}     #更新资产状态,数字
WHERE
	t.id = {assetId}            #资产ID，数字
	AND t.xm = '{name}'     #姓名，字符串
	AND t.sfzhm = '{idCard}'  #身份证号，字符串
";
        }

        public static string GetInsertToEntityTdbcLoanSql(string name, string idCard, string assetType, string assetId,
            string status, string loanId, string bankId, string loanAmount, string dueDate, string loanInterest,
            string txId)
        {
            var loadInterestPercent = "%" + loanInterest;
            return
                $@"INSERT INTO entity_tdbcloan ( loan_name, idcard, asset_type, asset_id, loan_status, loan_id, bank_id, loan_amount, due_date, loan_rate, transaction_id)
VALUES
	( '{name}',                 #姓名
	'{idCard}',      #身份证号
	{assetType},                        #资产类型
	{assetId},                      #资产ID
	'{status}',                       #状态
    '{loanId}',                        #银行贷款编号
	'{bankId}',      #银行标识
	{loanAmount},                 #放款金额
	'{dueDate}',              #到期日
	'{loadInterestPercent}',                   #贷款利率
    '{txId}'  #区块链交易事务ID 
)";
        }

        public static string GetInsertFileInfoSql(string name, string idCard, string assetType, string assetId, string fileId,
            string fileType, string fileHash, string transactionId)
        {
            return
                $@"INSERT INTO entity_tdbcloan_file ( loan_name, idcard, asset_type, asset_id, file_id, file_type, file_hash, transaction_id )
VALUES
	( '{name}', 
      '{idCard}', 
      {assetType}, 
      {assetId}, 
      '{fileId}',
      '{fileType}', 
      '{fileHash}', 
      '{transactionId}'
)";
        }

        public static string GetListSql(string name, string idCard, int assetId, int bfzt, string lsx,
            string lsxz, string lsc, int pageNo, int pageSize)
        {
            return $@"
select
 distinct a.id as asset_id,e.pc,e.pch,a.xmmc as xmmcid,d.xmmc as xmmcms,if(char_length(a.skr)=2,REPLACE(a.skr,SUBSTR(a.skr,1,1), '*'),REPLACE(a.skr,SUBSTR(a.skr,2,1), '*')) as name,INSERT(a.sfzh,7,10,'**********') as sfzh,if(length(a.lxfs)>0,CONCAT(LEFT(a.lxfs,3), '****' ,RIGHT(a.lxfs,4)),null) as lxfs,a.khyh,a.zhxz as zhxzid,case a.zhxz when 1 then '对私账号' when 2 then '对公账号' end as zhxzms,a.zchmj,a.bczje,a.bfzt,case a.bfzt when 1 then '拨付中' when 2 then '拨付成功' when 3 then '拨付失败' when 4 then '未发放' when 5 then '撤销成功' end as bfztms,e.lsxid,a.lsx,e.lsxzid,e.lsxz,e.lsc as lscid,f.name as lsc
            from entity_tdbchmc a left join entity_yxlx b on a.yhlx=b.id  /**关联银行**/
            left join entity_yxxxgl c on a.khszyh = c.id    /**关联开户行**/
            left join entity_xmxxgl d on a.xmmc = d.id      /**关联项目信息**/
            left join entity_bcmxsc e on a.tdbcmxpc = e.id  /**关联补偿批次信息**/
            left join lborganization f on f.id = e.lsc      /**关联县镇村地域信息**/
            where 1 = 1
" +
                   (string.IsNullOrEmpty(name)
                       ? ""
                       : $" and a.skr = '{name}'")
                   +
                   (string.IsNullOrEmpty(idCard)
                       ? ""
                       : $" and a.sfzh = '{idCard}'")
                   +
                   (assetId == 0
                       ? ""
                       : $" and a.id = {assetId}")
                   +
                   (bfzt < 1
                       ? ""
                       : $" and a.bfzt = {bfzt}")
                   +
                   (string.IsNullOrEmpty(lsx)
                       ? ""
                       : $" and e.lsxid = {lsx}")
                   +
                   (string.IsNullOrEmpty(lsxz)
                       ? ""
                       : $" and e.lsxzid = {lsxz}")
                   +
                   (string.IsNullOrEmpty(lsc)
                       ? ""
                       : $" and e.lsc = {lsc}")
                   +
                   $" order by a.id asc limit {pageNo},{pageSize}";
        }
        
        public static string GetListOfConstructionSql(string name, string idCard, int assetId, int pageNo, int pageSize)
        {
            return $@"
SELECT
t.id AS asset_id,	'' AS pc,	'' AS pch,	b.xmxx AS xmmcid,	c.xmmc AS xmmcms,
if(char_length(t.xm)=2,REPLACE(t.xm,SUBSTR(t.xm,1,1), '*'),REPLACE(t.xm,SUBSTR(t.xm,2,1), '*'))  AS name,	
            INSERT(t.sfzhm,7,10,'**********')  AS sfzh,	
            if(length(t.lxdh)>0,CONCAT(LEFT(t.lxdh,3), '****' ,RIGHT(t.lxdh,4)),null)  AS lxfs,	t.khhmc AS khyh,	'' AS zhxzid,
            '' AS zhxzms,	'' AS zchmj,	t.sfje AS bczje,
                CASE t.bfzt WHEN 1 THEN '待复核' WHEN 2 THEN '拨付中' WHEN 3 THEN '拨付失败' WHEN 4 THEN '已拨付' WHEN 5 THEN '录入失败' WHEN 6 THEN '工资代发' END AS bfztms,
            '' AS lsx,'' AS lsxz,t.ssxzjc AS lsc 
                FROM
            cet_zj_lwrygz_lwry t
            LEFT JOIN cet_zj_lwrygz b ON t.CET_ZJ_LWRYGZ_ID = b.id
            LEFT JOIN cet_xm_xmjbxx c ON b.xmxx = c.id 
            WHERE 1=1
" +
                   (string.IsNullOrEmpty(name)
                       ? ""
                       : $" and t.xm = '{name}'")
                   +
                   (string.IsNullOrEmpty(idCard)
                       ? ""
                       : $" and t.sfzhm = '{idCard}'")
                   +
                   (assetId == 0
                       ? ""
                       : $" and t.id = {assetId}")
                   +
                   "and t.bfzt<> 2 and replace(t.bfzt, 4,2) = 2"
                   +
                   $" order by t.id asc limit {pageNo},{pageSize}";
        }

        public static string GetDetailSql(string name, string idCard, int assetId)
        {
            return $@"
select
 a.id as asset_id,concat_ws('-',a.szzw,a.id) as blockId,a.id,if(char_length(a.skr)=2,REPLACE(a.skr,SUBSTR(a.skr,1,1), '*'),REPLACE(a.skr,SUBSTR(a.skr,2,1), '*')) as skr,INSERT(a.sfzh,7,10,'**********') as sfzh,if(length(a.lxfs)>0,CONCAT(LEFT(a.lxfs,3), '****' ,RIGHT(a.lxfs,4)),null) as lxfs,b.yhlx as yhlx,c.yhmc as khszyh,a.khyh,a.lhh,CONCAT(LEFT(yhzh,4), '****' ,RIGHT(yhzh,4)) as yhzh,
            case a.sfkh when 1 then '同行' when 2 then '跨行' end as sfkh,case a.zhxz when 1 then '对私账号' when 2 then '对公账号' end as zhxz,a.ntfw,a.dz,a.xz,a.nz,a.bf,a.zchmj, a.dtzw,a.sc,a.jjzw,a.sm,a.smz,a.dpzw,a.qt,a.tdsyj,a.dtzwje,a.scje,a.qtzwje,a.dpzwje,a.qtfzwje,a.bczje,a.zjedx,a.fj,a.bz,a.cjsj,a.cjr,case a.bfzt when 1 then '拨付中' when 2 then '拨付成功' when 3 then '拨付失败' when 4 then '未发放' when 5 then '撤销成功' end as bfzt,a.sfsc,a.htfj,a.tdbcmxpc,a.dealno,d.xmmc,e.xmlx,a.szzw,a.lsx,e.lsxz, (select name from lborganization where id = e.lsc) as lsc,case a.sfxx when 1 then '村委会' when 2 then '村民' end as sfxx
            from entity_tdbchmc a left join entity_yxlx b on a.yhlx=b.id  /**关联银行**/
            left join entity_yxxxgl c on a.khszyh = c.id    /**关联开户行**/
            left join entity_xmxxgl d on a.xmmc = d.id      /**关联项目信息**/
            left join entity_bcmxsc e on a.tdbcmxpc = e.id  /**关联补偿批次信息**/
            where a.skr = '{name}' and a.sfzh = '{idCard}' and a.id = {assetId}
";
        }
        
        public static string GetDetailOfConstructionSql(string name, string idCard, int assetId)
        {
           return $@"

SELECT 
	t.id AS asset_id,	concat_ws('-',t.qkgd,t.id) AS blockId,	t.id,	if(char_length(t.xm)=2,REPLACE(t.xm,SUBSTR(t.xm,1,1), '*'),REPLACE(t.xm,SUBSTR(t.xm,2,1), '*'))  AS skr,INSERT(t.sfzhm,7,10,'**********') AS sfzh,if(length(t.lxdh)>0,CONCAT(LEFT(t.lxdh,3), '****' ,RIGHT(t.lxdh,4)),null) AS lxfs,
            if(instr(t.khhmc,'银行')=0,'',substr(t.khhmc,1,instr(t.khhmc,'银行')+1)) as yhlx,t.khhmc AS khszyh,
                t.khhmc AS khyh,t.yhlhh as lhh,CONCAT(LEFT(t.yhkh,4), '****' ,RIGHT(t.yhkh,4)) as yhzh,case t.sfkh when 1 then '同行' when 2 then '跨行' end as sfkh,'' as zhxz,'' as ntfw,'' as dz,'' as xz,'' as nz,'' as bf,'' as zchmj,'' as dtzw,'' as sc,'' as jjzw,'' as sm,
            '' as smz,'' as dpzw,'' as qt,'' as tdsyj,'' as dtzwje,'' as scje,'' as qtzwje,'' as dpzwje,'' as qtfzwje,
            t.sfje AS bczje,'' as zjedx,'' as fj,t.bdbzxx as bz,b.casj as cjsj,b.czr as cjr,
            CASE t.bfzt WHEN 1 THEN '待复核' WHEN 2 THEN '拨付中' WHEN 3 THEN '拨付失败' WHEN 4 THEN '已拨付' WHEN 5 THEN '录入失败' WHEN 6 THEN '工资代发' END AS bfzt,
            '' as sfsc,'' as htfj,'' as tdbcmxpc,t.seqno as dealno,c.xmmc,'' as szzw,'' as lsx,'' as lsxz,t.ssxzjc as lsc,'' as sfxx
            FROM
                cet_zj_lwrygz_lwry t
                LEFT JOIN cet_zj_lwrygz b ON t.CET_ZJ_LWRYGZ_ID = b.id
            LEFT JOIN cet_xm_xmjbxx c ON b.xmxx = c.id 
            WHERE 1=1 
            AND t.xm = '{name}'                         #姓名	
            AND t.sfzhm = '{idCard}'                  #身份证号	
            AND t.id = {assetId}                           #资产ID

";
        }
    }
}