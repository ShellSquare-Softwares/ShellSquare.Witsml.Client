using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShellSquare.Witsml.Client
{
    internal static class Common
    {
        internal const string WELL_SIMPLE_PATH = @"\Template\Simple\Well.xml";
        internal const string WELL_IDS_PATH = @"\Template\Ids\WellId.xml";
        internal const string WELL_ALL_PATH = @"\Template\All\WellDetails.xml";

        internal const string WELLBORE_SIMPLE_PATH = @"\Template\Simple\Wellbore.xml";
        internal const string WELLBORE_IDS_PATH = @"\Template\Ids\WellboreId.xml";
        internal const string WELLBORE_ALL_PATH = @"\Template\All\WellboreDetails.xml";

        internal const string LOG_SIMPLE_PATH = @"\Template\Simple\Log.xml";
        internal const string LOG_IDS_PATH = @"\Template\Ids\LogId.xml";
        internal const string LOG_ALL_PATH = @"\Template\All\LogDetails.xml";

        internal const string ATTACHMENT_SIMPLE_PATH = @"\Template\Simple\attachment.xml";
        internal const string ATTACHMENT_IDS_PATH = @"\Template\Ids\attachmentId.xml";
        internal const string ATTACHMENT_ALL_PATH = @"\Template\All\attachmentDetails.xml";

        internal const string BHARUN_SIMPLE_PATH = @"\Template\Simple\bhaRun.xml";
        internal const string BHARUN_IDS_PATH = @"\Template\Ids\bhaRunId.xml";
        internal const string BHARUN_ALL_PATH = @"\Template\All\bhaRunDetails.xml";

        internal const string CEMENTJOB_SIMPLE_PATH = @"\Template\Simple\cementJob.xml";
        internal const string CEMENTJOB_IDS_PATH = @"\Template\Ids\cementJobId.xml";
        internal const string CEMENTJOB_ALL_PATH = @"\Template\All\cementJobDetails.xml";

        internal const string CHANGELOG_SIMPLE_PATH = @"\Template\Simple\changeLog.xml";
        internal const string CHANGELOG_IDS_PATH = @"\Template\Ids\changeLogId.xml";
        internal const string CHANGELOG_ALL_PATH = @"\Template\All\changeLogDetails.xml";

        internal const string CONVCORE_SIMPLE_PATH = @"\Template\Simple\convCore.xml";
        internal const string CONVCORE_IDS_PATH = @"\Template\Ids\convCoreId.xml";
        internal const string CONVCORE_ALL_PATH = @"\Template\All\convCoreDetails.xml";

        internal const string COORDINATEREFERENCE_SIMPLE_PATH = @"\Template\Simple\coordinateReference.xml";
        internal const string COORDINATEREFERENCE_IDS_PATH = @"\Template\Ids\coordinateReferenceId.xml";
        internal const string COORDINATEREFERENCE_ALL_PATH = @"\Template\All\coordinateReferenceDetails.xml";

        internal const string CUSTOMOBJECT_SIMPLE_PATH = @"\Template\Simple\customObject.xml";
        internal const string CUSTOMOBJECT_IDS_PATH = @"\Template\Ids\customObjectId.xml";
        internal const string CUSTOMOBJECT_ALL_PATH = @"\Template\All\customObjectDetails.xml";

        internal const string DRILLREPORT_SIMPLE_PATH = @"\Template\Simple\drillReport.xml";
        internal const string DRILLREPORT_IDS_PATH = @"\Template\Ids\drillReportId.xml";
        internal const string DRILLREPORT_ALL_PATH = @"\Template\All\drillReportDetails.xml";

        internal const string FLUIDSREPORT_SIMPLE_PATH = @"\Template\Simple\fluidsReport.xml";
        internal const string FLUIDSREPORT_IDS_PATH = @"\Template\Ids\fluidsReportId.xml";
        internal const string FLUIDSREPORT_ALL_PATH = @"\Template\All\fluidsReportDetails.xml";

        internal const string FORMATIONMARKER_SIMPLE_PATH = @"\Template\Simple\formationMarker.xml";
        internal const string FORMATIONMARKER_IDS_PATH = @"\Template\Ids\formationMarkerId.xml";
        internal const string FORMATIONMARKER_ALL_PATH = @"\Template\All\formationMarkerDetails.xml";

        internal const string MESSAGE_SIMPLE_PATH = @"\Template\Simple\message.xml";
        internal const string MESSAGE_IDS_PATH = @"\Template\Ids\messageId.xml";
        internal const string MESSAGE_ALL_PATH = @"\Template\All\messageDetails.xml";

        internal const string MNEMONICSET_SIMPLE_PATH = @"\Template\Simple\mnemonicSet.xml";
        internal const string MNEMONICSET_IDS_PATH = @"\Template\Ids\mnemonicSetId.xml";
        internal const string MNEMONICSET_ALL_PATH = @"\Template\All\mnemonicSetDetails.xml";

        internal const string MUDLOG_SIMPLE_PATH = @"\Template\Simple\mudLog.xml";
        internal const string MUDLOG_IDS_PATH = @"\Template\Ids\mudLogId.xml";
        internal const string MUDLOG_ALL_PATH = @"\Template\All\mudLogDetails.xml";

        internal const string OBJECTGROUP_SIMPLE_PATH = @"\Template\Simple\objectGroup.xml";
        internal const string OBJECTGROUP_IDS_PATH = @"\Template\Ids\objectGroupId.xml";
        internal const string OBJECTGROUP_ALL_PATH = @"\Template\All\objectGroupDetails.xml";

        internal const string OPSREPORT_SIMPLE_PATH = @"\Template\Simple\opsReport.xml";
        internal const string OPSREPORT_IDS_PATH = @"\Template\Ids\opsReportId.xml";
        internal const string OPSREPORT_ALL_PATH = @"\Template\All\opsReportDetails.xml";

        internal const string PRESSURETESTPLAN_SIMPLE_PATH = @"\Template\Simple\pressureTestPlan.xml";
        internal const string PRESSURETESTPLAN_IDS_PATH = @"\Template\Ids\pressureTestPlanId.xml";
        internal const string PRESSURETESTPLAN_ALL_PATH = @"\Template\All\pressureTestPlanDetails.xml";

        internal const string RIG_SIMPLE_PATH = @"\Template\Simple\rig.xml";
        internal const string RIG_IDS_PATH = @"\Template\Ids\rigId.xml";
        internal const string RIG_ALL_PATH = @"\Template\All\rigDetails.xml";

        internal const string RISK_SIMPLE_PATH = @"\Template\Simple\risk.xml";
        internal const string RISK_IDS_PATH = @"\Template\Ids\riskId.xml";
        internal const string RISK_ALL_PATH = @"\Template\All\riskDetails.xml";

        internal const string SIDEWALLCORE_SIMPLE_PATH = @"\Template\Simple\sideWallCore.xml";
        internal const string SIDEWALLCORE_IDS_PATH = @"\Template\Ids\sideWallCoreId.xml";
        internal const string SIDEWALLCORE_ALL_PATH = @"\Template\All\sideWallCoreDetails.xml";


        internal const string STIMJOB_SIMPLE_PATH = @"\Template\Simple\stimJob.xml";
        internal const string STIMJOB_IDS_PATH = @"\Template\Ids\stimJobId.xml";
        internal const string STIMJOB_ALL_PATH = @"\Template\All\stimJobDetails.xml";

        internal const string SURVEYPROGRAM_SIMPLE_PATH = @"\Template\Simple\surveyProgram.xml";
        internal const string SURVEYPROGRAM_IDS_PATH = @"\Template\Ids\surveyProgramId.xml";
        internal const string SURVEYPROGRAM_ALL_PATH = @"\Template\All\surveyProgramDetails.xml";

        internal const string TARGET_SIMPLE_PATH = @"\Template\Simple\target.xml";
        internal const string TARGET_IDS_PATH = @"\Template\Ids\targetId.xml";
        internal const string TARGET_ALL_PATH = @"\Template\All\targetDetails.xml";

        internal const string TOOLERRORMODEL_SIMPLE_PATH = @"\Template\Simple\toolErrorModel.xml";
        internal const string TOOLERRORMODEL_IDS_PATH = @"\Template\Ids\toolErrorModelId.xml";
        internal const string TOOLERRORMODEL_ALL_PATH = @"\Template\All\toolErrorModelDetails.xml";

        internal const string TOOLERRORTERMSET_SIMPLE_PATH = @"\Template\Simple\toolErrorTermSet.xml";
        internal const string TOOLERRORTERMSET_IDS_PATH = @"\Template\Ids\toolErrorTermSetId.xml";
        internal const string TOOLERRORTERMSET_ALL_PATH = @"\Template\All\toolErrorTermSetDetails.xml";

        internal const string TRAJECTORY_SIMPLE_PATH = @"\Template\Simple\trajectory.xml";
        internal const string TRAJECTORY_IDS_PATH = @"\Template\Ids\trajectoryId.xml";
        internal const string TRAJECTORY_ALL_PATH = @"\Template\All\trajectoryDetails.xml";

        internal const string TUBULAR_SIMPLE_PATH = @"\Template\Simple\tubular.xml";
        internal const string TUBULAR_IDS_PATH = @"\Template\Ids\tubularId.xml";
        internal const string TUBULAR_ALL_PATH = @"\Template\All\tubularDetails.xml";

        internal const string WBGEOMETRY_SIMPLE_PATH = @"\Template\Simple\wbGeometry.xml";
        internal const string WBGEOMETRY_IDS_PATH = @"\Template\Ids\wbGeometryId.xml";
        internal const string WBGEOMETRY_ALL_PATH = @"\Template\All\wbGeometryDetails.xml";

        internal const string TEMPLATE_INPUT_CONFIG_PATH = "Configuration.xml";


    }
}
