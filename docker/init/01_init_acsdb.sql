--
-- PostgreSQL database dump
--

-- Dumped from database version 18.3 (Debian 18.3-1.pgdg13+1)
-- Adapted for PostgreSQL 17

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

ALTER TABLE IF EXISTS ONLY public."NA_R_LOCATION" DROP CONSTRAINT IF EXISTS "uq_location_locationId";
ALTER TABLE IF EXISTS ONLY public."NA_X_OPTION" DROP CONSTRAINT IF EXISTS "PK_NA_X_OPTION";
ALTER TABLE IF EXISTS ONLY public."NA_X_APPLICATION_MANAGER" DROP CONSTRAINT IF EXISTS "PK_NA_X_APPLICATION_MANAGER";
ALTER TABLE IF EXISTS ONLY public."NA_X_APPLICATION" DROP CONSTRAINT IF EXISTS "PK_NA_X_APPLICATION";
ALTER TABLE IF EXISTS ONLY public."NA_U_TRANSPORT" DROP CONSTRAINT IF EXISTS "PK_NA_U_TRANSPORT";
ALTER TABLE IF EXISTS ONLY public."NA_U_INFORM" DROP CONSTRAINT IF EXISTS "PK_NA_U_INFORM";
ALTER TABLE IF EXISTS ONLY public."NA_U_COMMAND" DROP CONSTRAINT IF EXISTS "PK_NA_U_COMMAND";
ALTER TABLE IF EXISTS ONLY public."NA_T_INTERSECTION" DROP CONSTRAINT IF EXISTS "PK_NA_T_INTERSECTION";
ALTER TABLE IF EXISTS ONLY public."NA_T_CURRENTINTERSECTION" DROP CONSTRAINT IF EXISTS "PK_NA_T_CURRENTINTERSECTION";
ALTER TABLE IF EXISTS ONLY public."NA_R_VEHICLE_ORDER" DROP CONSTRAINT IF EXISTS "PK_NA_R_VEHICLE_ORDER";
ALTER TABLE IF EXISTS ONLY public."NA_R_VEHICLE_IDLE" DROP CONSTRAINT IF EXISTS "PK_NA_R_VEHICLE_IDLE";
ALTER TABLE IF EXISTS ONLY public."NA_R_VEHICLE_CROSS_WAIT" DROP CONSTRAINT IF EXISTS "PK_NA_R_VEHICLE_CROSS_WAIT";
ALTER TABLE IF EXISTS ONLY public."NA_R_STATION" DROP CONSTRAINT IF EXISTS "PK_NA_R_STATION";
ALTER TABLE IF EXISTS ONLY public."NA_R_SPECIALCONFIG" DROP CONSTRAINT IF EXISTS "PK_NA_R_SPECIALCONFIG";
ALTER TABLE IF EXISTS ONLY public."NA_R_ORDER_PAIR" DROP CONSTRAINT IF EXISTS "PK_NA_R_ORDER_PAIR";
ALTER TABLE IF EXISTS ONLY public."NA_R_NODE" DROP CONSTRAINT IF EXISTS "PK_NA_R_NODE";
ALTER TABLE IF EXISTS ONLY public."NA_R_LINK_ZONE" DROP CONSTRAINT IF EXISTS "PK_NA_R_LINK_ZONE";
ALTER TABLE IF EXISTS ONLY public."NA_R_LINK" DROP CONSTRAINT IF EXISTS "PK_NA_R_LINK";
ALTER TABLE IF EXISTS ONLY public."NA_Q_TRANSPORTCMDREQUEST" DROP CONSTRAINT IF EXISTS "PK_NA_Q_TRANSPORTCMDREQUEST";
ALTER TABLE IF EXISTS ONLY public."NA_M_CARRIER" DROP CONSTRAINT IF EXISTS "PK_NA_M_CARRIER";
ALTER TABLE IF EXISTS ONLY public."NA_L_LOGMESSAGE" DROP CONSTRAINT IF EXISTS "PK_NA_L_LOGMESSAGE";
ALTER TABLE IF EXISTS ONLY public."NA_L_LARGELOGMESSAGE" DROP CONSTRAINT IF EXISTS "PK_NA_L_LARGELOGMESSAGE";
ALTER TABLE IF EXISTS ONLY public."NA_H_VEHICLE_BATTERYHISTORY" DROP CONSTRAINT IF EXISTS "PK_NA_H_VEHICLE_BATTERYHISTORY";
ALTER TABLE IF EXISTS ONLY public."NA_H_VEHICLESEARCHPATH" DROP CONSTRAINT IF EXISTS "PK_NA_H_VEHICLESEARCHPATH";
ALTER TABLE IF EXISTS ONLY public."NA_H_VEHICLEHISTORY" DROP CONSTRAINT IF EXISTS "PK_NA_H_VEHICLEHISTORY";
ALTER TABLE IF EXISTS ONLY public."NA_H_TRANSPORTCMDHISTORY" DROP CONSTRAINT IF EXISTS "PK_NA_H_TRANSPORTCMDHISTORY";
ALTER TABLE IF EXISTS ONLY public."NA_H_NIOHISTORY" DROP CONSTRAINT IF EXISTS "PK_NA_H_NIOHISTORY";
ALTER TABLE IF EXISTS ONLY public."NA_H_MISSMATCHANDFLYHISTORY" DROP CONSTRAINT IF EXISTS "PK_NA_H_MISSMATCHANDFLYHISTORY";
ALTER TABLE IF EXISTS ONLY public."NA_H_HEARTBEATFAILHISTORY" DROP CONSTRAINT IF EXISTS "PK_NA_H_HEARTBEATFAILHISTORY";
ALTER TABLE IF EXISTS ONLY public."NA_H_CROSSWAIT_HISTORY" DROP CONSTRAINT IF EXISTS "PK_NA_H_CROSSWAIT_HISTORY";
ALTER TABLE IF EXISTS ONLY public."NA_H_ALARMTIMEHISTORY" DROP CONSTRAINT IF EXISTS "PK_NA_H_ALARMTIMEHISTORY";
ALTER TABLE IF EXISTS ONLY public."NA_H_ALARMRPTHISTORY" DROP CONSTRAINT IF EXISTS "PK_NA_H_ALARMRPTHISTORY";
ALTER TABLE IF EXISTS ONLY public."NA_C_NIO" DROP CONSTRAINT IF EXISTS "PK_NA_C_NIO";
ALTER TABLE IF EXISTS ONLY public."NA_A_ALARMSPEC" DROP CONSTRAINT IF EXISTS "PK_NA_A_ALARMSPEC";
ALTER TABLE IF EXISTS ONLY public."NA_A_ALARM" DROP CONSTRAINT IF EXISTS "PK_NA_A_ALARM";
ALTER TABLE IF EXISTS ONLY public."NA_T_TRANSPORTCMD" DROP CONSTRAINT IF EXISTS "NA_T_TRANSPORTCMD_pkey";
ALTER TABLE IF EXISTS ONLY public."NA_R_ZONE" DROP CONSTRAINT IF EXISTS "NA_R_ZONE_pkey";
ALTER TABLE IF EXISTS ONLY public."NA_R_VEHICLE" DROP CONSTRAINT IF EXISTS "NA_R_VEHICLE_pkey";
ALTER TABLE IF EXISTS ONLY public."NA_R_LOCATION" DROP CONSTRAINT IF EXISTS "NA_R_LOCATION_pkey";
ALTER TABLE IF EXISTS ONLY public."NA_R_BAY" DROP CONSTRAINT IF EXISTS "NA_R_BAY_pkey";
ALTER TABLE IF EXISTS ONLY public."NA_C_MQTT" DROP CONSTRAINT IF EXISTS "NA_C_MQTT_pkey";
ALTER TABLE IF EXISTS public."NA_T_TRANSPORTCMD" ALTER COLUMN id DROP DEFAULT;
ALTER TABLE IF EXISTS public."NA_R_ZONE" ALTER COLUMN id DROP DEFAULT;
ALTER TABLE IF EXISTS public."NA_R_VEHICLE" ALTER COLUMN id DROP DEFAULT;
ALTER TABLE IF EXISTS public."NA_R_LOCATION" ALTER COLUMN id DROP DEFAULT;
ALTER TABLE IF EXISTS public."NA_R_BAY" ALTER COLUMN id DROP DEFAULT;
DROP TABLE IF EXISTS public."NA_X_OPTION";
DROP TABLE IF EXISTS public."NA_X_APPLICATION_MANAGER";
DROP TABLE IF EXISTS public."NA_X_APPLICATION";
DROP TABLE IF EXISTS public."NA_U_TRANSPORT";
DROP TABLE IF EXISTS public."NA_U_INFORM";
DROP TABLE IF EXISTS public."NA_U_COMMAND";
DROP SEQUENCE IF EXISTS public."NA_T_TRANSPORTCMD_id_seq";
DROP TABLE IF EXISTS public."NA_T_TRANSPORTCMD";
DROP TABLE IF EXISTS public."NA_T_INTERSECTION";
DROP TABLE IF EXISTS public."NA_T_CURRENTINTERSECTION";
DROP SEQUENCE IF EXISTS public."NA_R_ZONE_id_seq";
DROP TABLE IF EXISTS public."NA_R_ZONE";
DROP SEQUENCE IF EXISTS public."NA_R_VEHICLE_id_seq";
DROP TABLE IF EXISTS public."NA_R_VEHICLE_ORDER";
DROP TABLE IF EXISTS public."NA_R_VEHICLE_IDLE";
DROP TABLE IF EXISTS public."NA_R_VEHICLE_CROSS_WAIT";
DROP TABLE IF EXISTS public."NA_R_VEHICLE";
DROP TABLE IF EXISTS public."NA_R_STATION";
DROP TABLE IF EXISTS public."NA_R_SPECIALCONFIG";
DROP TABLE IF EXISTS public."NA_R_ORDER_PAIR";
DROP TABLE IF EXISTS public."NA_R_NODE";
DROP SEQUENCE IF EXISTS public."NA_R_LOCATION_id_seq";
DROP TABLE IF EXISTS public."NA_R_LOCATION";
DROP TABLE IF EXISTS public."NA_R_LINK_ZONE";
DROP TABLE IF EXISTS public."NA_R_LINK";
DROP SEQUENCE IF EXISTS public."NA_R_BAY_id_seq";
DROP TABLE IF EXISTS public."NA_R_BAY";
DROP TABLE IF EXISTS public."NA_Q_TRANSPORTCMDREQUEST";
DROP TABLE IF EXISTS public."NA_M_CARRIER";
DROP TABLE IF EXISTS public."NA_L_LOGMESSAGE";
DROP TABLE IF EXISTS public."NA_L_LARGELOGMESSAGE";
DROP TABLE IF EXISTS public."NA_H_VEHICLE_BATTERYHISTORY";
DROP TABLE IF EXISTS public."NA_H_VEHICLESEARCHPATH";
DROP TABLE IF EXISTS public."NA_H_VEHICLEHISTORY";
DROP TABLE IF EXISTS public."NA_H_TRANSPORTCMDHISTORY";
DROP TABLE IF EXISTS public."NA_H_NIOHISTORY";
DROP TABLE IF EXISTS public."NA_H_MISSMATCHANDFLYHISTORY";
DROP TABLE IF EXISTS public."NA_H_HEARTBEATFAILHISTORY";
DROP TABLE IF EXISTS public."NA_H_CROSSWAIT_HISTORY";
DROP TABLE IF EXISTS public."NA_H_ALARMTIMEHISTORY";
DROP TABLE IF EXISTS public."NA_H_ALARMRPTHISTORY";
DROP TABLE IF EXISTS public."NA_C_NIO";
DROP TABLE IF EXISTS public."NA_C_MQTT";
DROP TABLE IF EXISTS public."NA_A_ALARMSPEC";
DROP TABLE IF EXISTS public."NA_A_ALARM";
SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: NA_A_ALARM; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_A_ALARM" (
    id character varying(64) NOT NULL,
    "alarmCode" character varying(64),
    "alarmText" character varying(255),
    "vehicleId" character varying(64),
    "createTime" timestamp with time zone,
    "alarmId" character varying(64),
    "transportCommandId" character varying(64),
    "nearAgv" character varying(64),
    "isCross" character varying(20)
);


--
-- Name: NA_A_ALARMSPEC; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_A_ALARMSPEC" (
    id character varying(64) NOT NULL,
    "alarmId" character varying(64),
    "alarmText" character varying(255),
    severity character varying(20),
    "Description1" text,
    description character varying(255),
    "CreateTime" timestamp with time zone,
    "EditTime" timestamp with time zone,
    "Creator" text,
    "Editor" text
);


--
-- Name: NA_C_MQTT; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_C_MQTT" (
    id SERIAL NOT NULL,
    "NAME" character varying(64),
    "applicationName" character varying(64),
    "workflowManagerName" character varying(255),
    "brokerIp" character varying(64),
    "brokerPort" integer DEFAULT 1883,
    "topicPrefix" character varying(128) DEFAULT 'amr/'::character varying,
    "clientId" character varying(128),
    "userName" character varying(64),
    password character varying(128),
    "keepAliveSeconds" integer DEFAULT 30,
    "reconnectDelayMs" integer DEFAULT 5000,
    state character varying(20) DEFAULT 'LOADED'::character varying,
    description character varying(255),
    "createTime" timestamp without time zone,
    creator character varying(45),
    editor character varying(45),
    "editTime" timestamp without time zone
);


--
-- Name: NA_C_NIO; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_C_NIO" (
    id character varying(64) NOT NULL,
    "interfaceClassName" character varying(255),
    "workflowManagerName" character varying(255),
    "applicationName" character varying(64),
    port integer NOT NULL,
    "remoteIp" character varying(64),
    "machineName" character varying(64),
    state character varying(20),
    description character varying(255),
    "createTime" timestamp with time zone,
    "editTime" timestamp with time zone,
    creator character varying(45),
    editor character varying(45),
    "NAME" character varying(64)
);


--
-- Name: NA_H_ALARMRPTHISTORY; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_H_ALARMRPTHISTORY" (
    id character varying(64) NOT NULL,
    "vehicleId" character varying(64),
    "alarmId" character varying(64),
    "alarmCode" character varying(64),
    "alarmText" character varying(255),
    state character varying(20),
    "transportCommandId" character varying(64),
    "partitionId" integer NOT NULL,
    "time" timestamp with time zone
);


--
-- Name: NA_H_ALARMTIMEHISTORY; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_H_ALARMTIMEHISTORY" (
    id character varying(64) NOT NULL,
    "alarmCode" character varying(64),
    "alarmText" character varying(255),
    "vehicleId" character varying(64),
    "alarmId" character varying(64),
    "createTime" timestamp with time zone,
    "clearTime" timestamp with time zone,
    "transportCommandId" character varying(64),
    "nearAgv" character varying(64),
    "bayId" character varying(64),
    "isCross" character varying(20),
    "partitionId" integer NOT NULL,
    "time" timestamp with time zone
);


--
-- Name: NA_H_CROSSWAIT_HISTORY; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_H_CROSSWAIT_HISTORY" (
    id character varying(64) NOT NULL,
    "vehicleId" character varying(64),
    "nodeId" character varying(64),
    state character varying(20),
    "createTime" timestamp with time zone NOT NULL,
    "permitTime" timestamp with time zone NOT NULL,
    "crossWaitSeconds" integer NOT NULL,
    "partitionId" integer NOT NULL,
    "time" timestamp with time zone
);


--
-- Name: NA_H_HEARTBEATFAILHISTORY; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_H_HEARTBEATFAILHISTORY" (
    id character varying(64) NOT NULL,
    "applicationName" character varying(64),
    type character varying(20),
    state character varying(20),
    "startTime" timestamp with time zone,
    "initialHardware" character varying(64),
    "runningHardware" character varying(64),
    "partitionId" integer NOT NULL,
    "time" timestamp with time zone
);


--
-- Name: NA_H_MISSMATCHANDFLYHISTORY; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_H_MISSMATCHANDFLYHISTORY" (
    id character varying(64) NOT NULL,
    "vehicleId" character varying(64),
    "currentNodeId" character varying(64),
    "ngNode" character varying(64),
    type character varying(20),
    "partitionId" integer NOT NULL,
    "time" timestamp with time zone
);


--
-- Name: NA_H_NIOHISTORY; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_H_NIOHISTORY" (
    id character varying(64) NOT NULL,
    name character varying(64),
    state character varying(20),
    "machineName" character varying(64),
    "remoteIp" character varying(64),
    port integer NOT NULL,
    "applicationName" character varying(64),
    location character varying(255),
    "partitionId" integer NOT NULL,
    "time" timestamp with time zone
);


--
-- Name: NA_H_TRANSPORTCMDHISTORY; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_H_TRANSPORTCMDHISTORY" (
    id character varying(64) NOT NULL,
    "jobId" character varying(64),
    priority integer NOT NULL,
    state character varying(20),
    "vehicleId" character varying(64),
    "vehicleEvent" character varying(64),
    "carrierId" character varying(64),
    source character varying(64),
    dest character varying(64),
    path character varying(2000),
    "additionalInfo" character varying(1000),
    "createTime" timestamp with time zone,
    "queuedTime" timestamp with time zone,
    "assignedTime" timestamp with time zone,
    "startedTime" timestamp with time zone,
    "loadArrivedTime" timestamp with time zone,
    "loadedTime" timestamp with time zone,
    "unloadArrivedTime" timestamp with time zone,
    "unloadedTime" timestamp with time zone,
    "loadingTime" timestamp with time zone,
    "unloadingTime" timestamp with time zone,
    "completedTime" timestamp with time zone,
    "eqpId" character varying(64),
    "portId" character varying(64),
    "agvName" character varying(64),
    "jobType" character varying(64),
    "midLoc" character varying(64),
    "midPortId" character varying(64),
    "originLoc" character varying(64),
    description character varying(256),
    "bayId" character varying(64),
    reason character varying(255),
    code character varying(64),
    "partitionId" integer NOT NULL,
    "time" timestamp with time zone
);


--
-- Name: NA_H_VEHICLEHISTORY; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_H_VEHICLEHISTORY" (
    id character varying(64) NOT NULL,
    "vehicleId" character varying(64),
    "bayId" character varying(64),
    "carrierType" character varying(8),
    "connectionState" character varying(16),
    "alarmState" character varying(8),
    "processingState" character varying(20),
    "currentNodeId" character varying(64),
    "transportCommandId" character varying(64),
    path character varying(2000),
    "nodeCheckTime" timestamp with time zone,
    state character varying(20),
    installed character varying(20),
    "transferState" character varying(20),
    "runState" character varying(10),
    "fullState" character varying(10),
    "messageName" character varying(64),
    "acsDestNodeId" character varying(64),
    "vehicleDestNodeId" character varying(64),
    "partitionId" integer NOT NULL,
    "time" timestamp with time zone
);


--
-- Name: NA_H_VEHICLESEARCHPATH; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_H_VEHICLESEARCHPATH" (
    id character varying(64) NOT NULL,
    "TransferState" text,
    "Distance" integer NOT NULL,
    "TrCmd" text,
    "Type" text,
    "vehicleId" character varying(64),
    "bayId" character varying(64),
    "currentNodeId" character varying(64),
    path character varying(2000),
    "partitionId" integer NOT NULL,
    "time" timestamp with time zone
);


--
-- Name: NA_H_VEHICLE_BATTERYHISTORY; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_H_VEHICLE_BATTERYHISTORY" (
    id character varying(64) NOT NULL,
    "vehicleId" character varying(64),
    "batteryRate" integer NOT NULL,
    "batteryVoltage" real NOT NULL,
    "processingState" character varying(20),
    "partitionId" integer NOT NULL,
    "time" timestamp with time zone
);


--
-- Name: NA_L_LARGELOGMESSAGE; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_L_LARGELOGMESSAGE" (
    id character varying(64) NOT NULL,
    "logMessageId" character varying(64),
    "largeText" text,
    sequence integer NOT NULL,
    "partitionId" integer NOT NULL,
    "time" timestamp with time zone
);


--
-- Name: NA_L_LOGMESSAGE; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_L_LOGMESSAGE" (
    id character varying(64) NOT NULL,
    "transactionId" character varying(64),
    "threadName" character varying(64),
    "operationName" character varying(128),
    "processName" character varying(64),
    "messageName" character varying(64),
    "communicationMessageName" character varying(64),
    "transportCommandId" character varying(64),
    "carrierName" character varying(64),
    "machineName" character varying(64),
    "unitName" character varying(64),
    text character varying(4000),
    "logLevel" character varying(20),
    "WorkflowLog" boolean NOT NULL,
    "SaveToDatabase" boolean NOT NULL,
    "partitionId" integer NOT NULL,
    "time" timestamp with time zone
);


--
-- Name: NA_M_CARRIER; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_M_CARRIER" (
    id character varying(64) NOT NULL,
    type character varying(20),
    "carrierLoc" character varying(64),
    "createTime" timestamp with time zone
);


--
-- Name: NA_Q_TRANSPORTCMDREQUEST; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_Q_TRANSPORTCMDREQUEST" (
    id character varying(64) NOT NULL,
    "messageName" character varying(64),
    "jobId" character varying(64),
    "vehicleId" character varying(64),
    dest character varying(64),
    description character varying(255),
    "createTime" timestamp with time zone
);


--
-- Name: NA_R_BAY; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_R_BAY" (
    floor integer NOT NULL,
    description character varying(255),
    "agvType" character varying(20),
    "chargeVoltage" real NOT NULL,
    "limitVoltage" real NOT NULL,
    "idleTime" integer NOT NULL,
    "ZoneMove" text,
    "Traffic" text,
    "StopOut" text,
    "bayId" character varying(64),
    id bigint NOT NULL
);


--
-- Name: NA_R_BAY_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public."NA_R_BAY_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: NA_R_BAY_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public."NA_R_BAY_id_seq" OWNED BY public."NA_R_BAY".id;


--
-- Name: NA_R_LINK; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_R_LINK" (
    id character varying(64) NOT NULL,
    "fromNodeId" character varying(64),
    "toNodeId" character varying(64),
    availability character varying(20),
    length integer NOT NULL,
    speed integer NOT NULL,
    "agvType" character varying(20),
    load integer NOT NULL,
    "leftBranch" integer NOT NULL
);


--
-- Name: NA_R_LINK_ZONE; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_R_LINK_ZONE" (
    id character varying(64) NOT NULL,
    "linkId" character varying(64),
    "zoneId" character varying(64),
    "transferFlag" character varying(20)
);


--
-- Name: NA_R_LOCATION; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_R_LOCATION" (
    "locationId" character varying(64) CONSTRAINT "NA_R_LOCATION_portId_not_null" NOT NULL,
    "stationId" character varying(64),
    type character varying(8),
    "carrierType" character varying(8),
    state character varying(20),
    direction character varying(8),
    id bigint NOT NULL
);


--
-- Name: NA_R_LOCATION_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public."NA_R_LOCATION_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: NA_R_LOCATION_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public."NA_R_LOCATION_id_seq" OWNED BY public."NA_R_LOCATION".id;


--
-- Name: NA_R_NODE; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_R_NODE" (
    id serial NOT NULL,
    node_id character varying(64) NOT NULL,
    type character varying(20),
    xpos double precision NOT NULL,
    ypos double precision NOT NULL,
    zpos double precision NOT NULL
);


--
-- Name: NA_R_ORDER_PAIR; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_R_ORDER_PAIR" (
    id character varying(64) NOT NULL,
    "orderGroup" character varying(64),
    status character varying(20)
);


--
-- Name: NA_R_SPECIALCONFIG; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_R_SPECIALCONFIG" (
    "ID" character varying(20) NOT NULL,
    "NAME" character varying(64),
    "VALUES" character varying(255)
);


--
-- Name: NA_R_STATION; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_R_STATION" (
    id character varying(64) NOT NULL,
    "linkId" character varying(64),
    type character varying(20),
    distance integer NOT NULL,
    "Direction" text
);


--
-- Name: NA_R_VEHICLE; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_R_VEHICLE" (
    "LASTCHARGETIME" timestamp with time zone NOT NULL,
    "LASTCHARGEBATTERY" real NOT NULL,
    "COMMID" character varying(32),
    vendor character varying(32),
    version character varying(32),
    "bayId" character varying(64),
    "carrierType" character varying(8),
    "connectionState" character varying(16),
    "alarmState" character varying(8),
    "processingState" character varying(20),
    "runState" character varying(10),
    "fullState" character varying(10),
    state character varying(20),
    "batteryRate" integer NOT NULL,
    "batteryVoltage" real NOT NULL,
    "currentNodeId" character varying(64),
    "acsDestNodeId" character varying(64),
    "vehicleDestNodeId" character varying(64),
    "transportCommandId" character varying(64),
    path character varying(2000),
    "nodeCheckTime" timestamp with time zone NOT NULL,
    installed character varying(20),
    "transferState" character varying(20),
    "eventTime" timestamp with time zone NOT NULL,
    "plcVersion" character varying(32),
    "vehicleId" character varying(64),
    id bigint NOT NULL,
    "COMMTYPE" character varying(10) DEFAULT 'NIO'::character varying
);


--
-- Name: NA_R_VEHICLE_CROSS_WAIT; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_R_VEHICLE_CROSS_WAIT" (
    "vehicleId" character varying(64) NOT NULL,
    "nodeId" character varying(64),
    state character varying(20),
    "createdTime" timestamp with time zone NOT NULL,
    "Id" text
);


--
-- Name: NA_R_VEHICLE_IDLE; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_R_VEHICLE_IDLE" (
    id character varying(64) NOT NULL,
    "bayId" character varying(64),
    "idleTime" timestamp with time zone NOT NULL,
    "vehicleId" character varying(64)
);


--
-- Name: NA_R_VEHICLE_ORDER; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_R_VEHICLE_ORDER" (
    id character varying(64) NOT NULL,
    "Reply" text,
    "vehicleId" character varying(64),
    "orderTime" timestamp with time zone,
    "orderNode" character varying(64)
);


--
-- Name: NA_R_VEHICLE_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public."NA_R_VEHICLE_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: NA_R_VEHICLE_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public."NA_R_VEHICLE_id_seq" OWNED BY public."NA_R_VEHICLE".id;


--
-- Name: NA_R_ZONE; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_R_ZONE" (
    "bayId" character varying(64),
    description character varying(255),
    "zoneId" character varying(64),
    id bigint NOT NULL
);


--
-- Name: NA_R_ZONE_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public."NA_R_ZONE_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: NA_R_ZONE_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public."NA_R_ZONE_id_seq" OWNED BY public."NA_R_ZONE".id;


--
-- Name: NA_T_CURRENTINTERSECTION; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_T_CURRENTINTERSECTION" (
    id character varying(64) NOT NULL,
    "currentDirectionNode" character varying(64),
    "changedTime" timestamp with time zone,
    state character varying(20)
);


--
-- Name: NA_T_INTERSECTION; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_T_INTERSECTION" (
    id character varying(64) NOT NULL,
    "interSectionId" character varying(64),
    "checkPreviousNode" character varying(20),
    "startNodeId" character varying(64),
    "endNodeId" character varying(64),
    "interval" integer NOT NULL,
    sequence integer NOT NULL,
    "checkNodeIds" character varying(2000),
    "previousNodeIds" character varying(2000)
);


--
-- Name: NA_T_TRANSPORTCMD; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_T_TRANSPORTCMD" (
    priority integer NOT NULL,
    state character varying(20),
    "vehicleId" character varying(64),
    "vehicleEvent" character varying(64),
    "carrierId" character varying(64),
    source character varying(64),
    dest character varying(64),
    path character varying(2000),
    "additionalInfo" character varying(1000),
    "createdTime" timestamp with time zone,
    "queuedTime" timestamp with time zone,
    "assignedTime" timestamp with time zone,
    "loadArrivedTime" timestamp with time zone,
    "loadedTime" timestamp with time zone,
    "unloadArrivedTime" timestamp with time zone,
    "unloadedTime" timestamp with time zone,
    "loadingTime" timestamp with time zone,
    "unloadingTime" timestamp with time zone,
    "completedTime" timestamp with time zone,
    "startedTime" timestamp with time zone,
    "eqpId" character varying(64),
    "portId" character varying(64),
    "agvName" character varying(64),
    "jobType" character varying(64),
    "midLoc" character varying(64),
    "midPortId" character varying(64),
    "originLoc" character varying(64),
    description character varying(256),
    "bayId" character varying(64),
    "jobId" character varying(64),
    id bigint NOT NULL
);


--
-- Name: NA_T_TRANSPORTCMD_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public."NA_T_TRANSPORTCMD_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: NA_T_TRANSPORTCMD_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public."NA_T_TRANSPORTCMD_id_seq" OWNED BY public."NA_T_TRANSPORTCMD".id;


--
-- Name: NA_U_COMMAND; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_U_COMMAND" (
    "ID" character varying(64) NOT NULL,
    "MESSAGENAME" character varying(64),
    "APPLICATIONNAME" character varying(64),
    "APPLICATIONTYPE" character varying(64),
    "USERID" character varying(64),
    "CAUSE" character varying(255),
    "DESCRIPTION" character varying(255),
    "TIME" timestamp with time zone,
    "CreateTime" timestamp with time zone,
    "EditTime" timestamp with time zone,
    "Creator" text,
    "Editor" text,
    "Name" text
);


--
-- Name: NA_U_INFORM; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_U_INFORM" (
    id character varying(64) NOT NULL,
    "time" timestamp with time zone NOT NULL,
    type character varying(20),
    message character varying(255),
    source character varying(64),
    description character varying(255)
);


--
-- Name: NA_U_TRANSPORT; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_U_TRANSPORT" (
    "ID" character varying(64) NOT NULL,
    "MESSAGENAME" character varying(64),
    "TRANSPORTCOMMANDID" character varying(64),
    "SOURCEPORTID" character varying(64),
    "DESTPORTID" character varying(64),
    "VEHICLEID" character varying(64),
    "DESTNODEID" character varying(64),
    "REQUESTID" character varying(64),
    "USERID" character varying(64),
    "CAUSE" character varying(255),
    "DESCRIPTION" character varying(255),
    "TIME" timestamp with time zone
);


--
-- Name: NA_X_APPLICATION; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_X_APPLICATION" (
    id character varying(64) NOT NULL,
    type character varying(20),
    state character varying(20),
    "startTime" timestamp with time zone,
    "checkTime" timestamp with time zone,
    "initialHardware" character varying(64),
    "runningHardware" character varying(64),
    "runningHardwareAddress" character varying(64),
    msb character varying(20),
    memory character varying(64),
    jmx character varying(20),
    "destinationName" character varying(64),
    description character varying(255),
    "createTime" timestamp with time zone,
    "editTime" timestamp with time zone,
    creator character varying(45),
    editor character varying(45),
    "NAME" character varying(64)
);


--
-- Name: NA_X_APPLICATION_MANAGER; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_X_APPLICATION_MANAGER" (
    "ID" character varying(64) NOT NULL,
    "TYPE" character varying(20),
    "COMMAND" character varying(64),
    "REPLY" character varying(255),
    "STATE" character varying(20),
    "USERID" character varying(64),
    "IPADDRESS" character varying(64),
    "EVENTTIME" timestamp with time zone,
    "REQUESTTIME" timestamp with time zone
);


--
-- Name: NA_X_OPTION; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NA_X_OPTION" (
    id character varying(64) NOT NULL,
    name character varying(64),
    "nameDescription" character varying(255),
    value character varying(64),
    "valueDescription" character varying(255),
    "subValue" character varying(64),
    "subValueDescription" character varying(255),
    used character varying(10)
);


--
-- Name: NA_R_BAY id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_R_BAY" ALTER COLUMN id SET DEFAULT nextval('public."NA_R_BAY_id_seq"'::regclass);


--
-- Name: NA_R_LOCATION id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_R_LOCATION" ALTER COLUMN id SET DEFAULT nextval('public."NA_R_LOCATION_id_seq"'::regclass);


--
-- Name: NA_R_VEHICLE id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_R_VEHICLE" ALTER COLUMN id SET DEFAULT nextval('public."NA_R_VEHICLE_id_seq"'::regclass);


--
-- Name: NA_R_ZONE id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_R_ZONE" ALTER COLUMN id SET DEFAULT nextval('public."NA_R_ZONE_id_seq"'::regclass);


--
-- Name: NA_T_TRANSPORTCMD id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_T_TRANSPORTCMD" ALTER COLUMN id SET DEFAULT nextval('public."NA_T_TRANSPORTCMD_id_seq"'::regclass);


--
-- Data for Name: NA_A_ALARM; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_A_ALARM" (id, "alarmCode", "alarmText", "vehicleId", "createTime", "alarmId", "transportCommandId", "nearAgv", "isCross") FROM stdin;
\.


--
-- Data for Name: NA_A_ALARMSPEC; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_A_ALARMSPEC" (id, "alarmId", "alarmText", severity, "Description1", description, "CreateTime", "EditTime", "Creator", "Editor") FROM stdin;
\.


--
-- Data for Name: NA_C_MQTT; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_C_MQTT" ("NAME", "applicationName", "workflowManagerName", "brokerIp", "brokerPort", "topicPrefix", "clientId", "userName", password, "keepAliveSeconds", "reconnectDelayMs", state, description, "createTime", creator, editor, "editTime") FROM stdin;
MQTT_CFG01	ES01_P	elsaWorkflowManager	localhost	1883	amr/	ACS_EI_01	guest	guest	30	5000	CONNECTED	\N	2026-03-25 05:10:22.970062	admin	admin	2026-03-26 00:25:01.313745
\.


--
-- Data for Name: NA_C_NIO; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_C_NIO" (id, "interfaceClassName", "workflowManagerName", "applicationName", port, "remoteIp", "machineName", state, description, "createTime", "editTime", creator, editor, "NAME") FROM stdin;
\.


--
-- Data for Name: NA_H_ALARMRPTHISTORY; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_H_ALARMRPTHISTORY" (id, "vehicleId", "alarmId", "alarmCode", "alarmText", state, "transportCommandId", "partitionId", "time") FROM stdin;
\.


--
-- Data for Name: NA_H_ALARMTIMEHISTORY; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_H_ALARMTIMEHISTORY" (id, "alarmCode", "alarmText", "vehicleId", "alarmId", "createTime", "clearTime", "transportCommandId", "nearAgv", "bayId", "isCross", "partitionId", "time") FROM stdin;
\.


--
-- Data for Name: NA_H_CROSSWAIT_HISTORY; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_H_CROSSWAIT_HISTORY" (id, "vehicleId", "nodeId", state, "createTime", "permitTime", "crossWaitSeconds", "partitionId", "time") FROM stdin;
\.


--
-- Data for Name: NA_H_HEARTBEATFAILHISTORY; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_H_HEARTBEATFAILHISTORY" (id, "applicationName", type, state, "startTime", "initialHardware", "runningHardware", "partitionId", "time") FROM stdin;
\.


--
-- Data for Name: NA_H_MISSMATCHANDFLYHISTORY; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_H_MISSMATCHANDFLYHISTORY" (id, "vehicleId", "currentNodeId", "ngNode", type, "partitionId", "time") FROM stdin;
\.


--
-- Data for Name: NA_H_NIOHISTORY; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_H_NIOHISTORY" (id, name, state, "machineName", "remoteIp", port, "applicationName", location, "partitionId", "time") FROM stdin;
\.


--
-- Data for Name: NA_H_TRANSPORTCMDHISTORY; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_H_TRANSPORTCMDHISTORY" (id, "jobId", priority, state, "vehicleId", "vehicleEvent", "carrierId", source, dest, path, "additionalInfo", "createTime", "queuedTime", "assignedTime", "startedTime", "loadArrivedTime", "loadedTime", "unloadArrivedTime", "unloadedTime", "loadingTime", "unloadingTime", "completedTime", "eqpId", "portId", "agvName", "jobType", "midLoc", "midPortId", "originLoc", description, "bayId", reason, code, "partitionId", "time") FROM stdin;
\.


--
-- Data for Name: NA_H_VEHICLEHISTORY; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_H_VEHICLEHISTORY" (id, "vehicleId", "bayId", "carrierType", "connectionState", "alarmState", "processingState", "currentNodeId", "transportCommandId", path, "nodeCheckTime", state, installed, "transferState", "runState", "fullState", "messageName", "acsDestNodeId", "vehicleDestNodeId", "partitionId", "time") FROM stdin;
\.


--
-- Data for Name: NA_H_VEHICLESEARCHPATH; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_H_VEHICLESEARCHPATH" (id, "TransferState", "Distance", "TrCmd", "Type", "vehicleId", "bayId", "currentNodeId", path, "partitionId", "time") FROM stdin;
\.


--
-- Data for Name: NA_H_VEHICLE_BATTERYHISTORY; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_H_VEHICLE_BATTERYHISTORY" (id, "vehicleId", "batteryRate", "batteryVoltage", "processingState", "partitionId", "time") FROM stdin;
\.


--
-- Data for Name: NA_L_LARGELOGMESSAGE; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_L_LARGELOGMESSAGE" (id, "logMessageId", "largeText", sequence, "partitionId", "time") FROM stdin;
\.


--
-- Data for Name: NA_L_LOGMESSAGE; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_L_LOGMESSAGE" (id, "transactionId", "threadName", "operationName", "processName", "messageName", "communicationMessageName", "transportCommandId", "carrierName", "machineName", "unitName", text, "logLevel", "WorkflowLog", "SaveToDatabase", "partitionId", "time") FROM stdin;
\.


--
-- Data for Name: NA_M_CARRIER; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_M_CARRIER" (id, type, "carrierLoc", "createTime") FROM stdin;
\.


--
-- Data for Name: NA_Q_TRANSPORTCMDREQUEST; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_Q_TRANSPORTCMDREQUEST" (id, "messageName", "jobId", "vehicleId", dest, description, "createTime") FROM stdin;
\.


--
-- Data for Name: NA_R_BAY; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_R_BAY" (floor, description, "agvType", "chargeVoltage", "limitVoltage", "idleTime", "ZoneMove", "Traffic", "StopOut", "bayId", id) FROM stdin;
1	테스트 BAY		28	21	0				DEMO	1
\.


--
-- Data for Name: NA_R_LINK; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_R_LINK" (id, "fromNodeId", "toNodeId", availability, length, speed, "agvType", load, "leftBranch") FROM stdin;
N012_N001	N012	N001	0	0	0	\N	0	0
N001_N002	N001	N002	0	0	0	\N	0	0
N002_N003	N002	N003	0	0	0	\N	0	0
N003_N004	N003	N004	0	0	0	\N	0	0
N004_N005	N004	N005	0	0	0	\N	0	0
N005_N006	N005	N006	0	0	0	\N	0	0
N006_N007	N006	N007	0	0	0	\N	0	0
N007_N008	N007	N008	0	0	0	\N	0	0
N008_N009	N008	N009	0	0	0	\N	0	0
N009_N010	N009	N010	0	0	0	\N	0	0
N010_N011	N010	N011	0	0	0	\N	0	0
N011_N012	N011	N012	0	0	0	\N	0	0
\.


--
-- Data for Name: NA_R_LINK_ZONE; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_R_LINK_ZONE" (id, "linkId", "zoneId", "transferFlag") FROM stdin;
N012_N001_DEMO	N012_N001	DEMO	Y
N001_N002_DEMO	N001_N002	DEMO	Y
N003_N004_DEMO	N003_N004	DEMO	Y
N004_N005_DEMO	N004_N005	DEMO	Y
N005_N006_DEMO	N005_N006	DEMO	Y
N006_N007_DEMO	N006_N007	DEMO	Y
N007_N008_DEMO	N007_N008	DEMO	Y
N008_N009_DEMO	N008_N009	DEMO	Y
N009_N010_DEMO	N009_N010	DEMO	Y
N010_N011_DEMO	N010_N011	DEMO	Y
N011_N012_DEMO	N011_N012	DEMO	Y
N002_N003_DEMO	N002_N003	DEMO	Y
\.


--
-- Data for Name: NA_R_LOCATION; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_R_LOCATION" ("locationId", "stationId", type, "carrierType", state, direction, id) FROM stdin;
192.168.1.101:LEFT	N001	EQP	MAGAZINE		LEFT	1
192.168.1.101:RIGHT	N001	EQP	MAGAZINE		RIGHT	2
192.168.1.103:LEFT	N003	EQP	MAGAZINE		LEFT	5
192.168.1.102:RIGHT	N002	EQP	MAGAZINE		RIGHT	4
192.168.1.102:LEFT	N002	EQP	MAGAZINE		LEFT	3
192.168.1.103:RIGHT	N003	EQP	MAGAZINE		RIGHT	6
\.


--
-- Data for Name: NA_R_NODE; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_R_NODE" (node_id, type, xpos, ypos, zpos) FROM stdin;
N003	COMMON	34	-263	0
N004	COMMON	269	-267	0
N005	COMMON	467	-264	0
N006	COMMON	466	70	0
N007	COMMON	275	73	0
N008	COMMON	32	67	0
N009	COMMON	-213	79	0
N010	COMMON	-458	75	0
N001	COMMON	-444	-262	0
N002	COMMON	-201	-261	0
N011	COMMON	-790	78	0
N012	COMMON	-769	-267	0
\.


--
-- Data for Name: NA_R_ORDER_PAIR; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_R_ORDER_PAIR" (id, "orderGroup", status) FROM stdin;
\.


--
-- Data for Name: NA_R_SPECIALCONFIG; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_R_SPECIALCONFIG" ("ID", "NAME", "VALUES") FROM stdin;
\.


--
-- Data for Name: NA_R_STATION; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_R_STATION" (id, "linkId", type, distance, "Direction") FROM stdin;
N001	N001_N002	BOTH	0	
N002	N002_N003	BOTH	0	
N003	N003_N004	BOTH	0	LEFT
\.


--
-- Data for Name: NA_R_VEHICLE; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_R_VEHICLE" ("LASTCHARGETIME", "LASTCHARGEBATTERY", "COMMID", vendor, version, "bayId", "carrierType", "connectionState", "alarmState", "processingState", "runState", "fullState", state, "batteryRate", "batteryVoltage", "currentNodeId", "acsDestNodeId", "vehicleDestNodeId", "transportCommandId", path, "nodeCheckTime", installed, "transferState", "eventTime", "plcVersion", "vehicleId", id, "COMMTYPE") FROM stdin;
2026-03-22 12:04:33.953965+00	0	AMR001	\N	\N	DEMO	\N	CONNECT	NOALARM	IDLE	RUN	EMPTY	INSTALLED	70	25	0000	\N	\N	\N	\N	2026-03-22 12:04:33.953902+00	T	NOTASSIGNED	2026-03-25 15:26:41.876469+00	\N	001	1	MQTT
\.


--
-- Data for Name: NA_R_VEHICLE_CROSS_WAIT; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_R_VEHICLE_CROSS_WAIT" ("vehicleId", "nodeId", state, "createdTime", "Id") FROM stdin;
\.


--
-- Data for Name: NA_R_VEHICLE_IDLE; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_R_VEHICLE_IDLE" (id, "bayId", "idleTime", "vehicleId") FROM stdin;
\.


--
-- Data for Name: NA_R_VEHICLE_ORDER; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_R_VEHICLE_ORDER" (id, "Reply", "vehicleId", "orderTime", "orderNode") FROM stdin;
\.


--
-- Data for Name: NA_R_ZONE; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_R_ZONE" ("bayId", description, "zoneId", id) FROM stdin;
DEMO		DEMO	1
\.


--
-- Data for Name: NA_T_CURRENTINTERSECTION; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_T_CURRENTINTERSECTION" (id, "currentDirectionNode", "changedTime", state) FROM stdin;
\.


--
-- Data for Name: NA_T_INTERSECTION; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_T_INTERSECTION" (id, "interSectionId", "checkPreviousNode", "startNodeId", "endNodeId", "interval", sequence, "checkNodeIds", "previousNodeIds") FROM stdin;
\.


--
-- Data for Name: NA_T_TRANSPORTCMD; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_T_TRANSPORTCMD" (priority, state, "vehicleId", "vehicleEvent", "carrierId", source, dest, path, "additionalInfo", "createdTime", "queuedTime", "assignedTime", "loadArrivedTime", "loadedTime", "unloadArrivedTime", "unloadedTime", "loadingTime", "unloadingTime", "completedTime", "startedTime", "eqpId", "portId", "agvName", "jobType", "midLoc", "midPortId", "originLoc", description, "bayId", "jobId", id) FROM stdin;
3	QUEUED	\N	\N	\N		192.168.1.11:LEFT	\N	\N	2026-03-20 03:53:44.461783+00	2026-03-20 03:53:44.461785+00	\N	\N	\N	\N	\N	\N	\N	\N	\N	ACS01	\N	\N	LOAD	\N	\N	\N	MAGAZINE	\N	JOB001	1
3	QUEUED	\N	\N	\N		192.168.1.123:RIGHT	\N	\N	2026-03-21 12:37:20.14954+00	2026-03-21 12:37:20.14954+00	\N	\N	\N	\N	\N	\N	\N	\N	\N	ACS01	\N	\N	LOAD	\N	\N	\N	MAGAZINE	\N	JOB002	2
3	QUEUED	\N	\N	\N	192.168.1.101:LEFT	192.168.1.103:RIGHT	\N	\N	2026-03-24 16:11:33.486925+00	2026-03-24 16:11:33.486926+00	\N	\N	\N	\N	\N	\N	\N	\N	\N	ACS01	\N	\N	LOAD	\N	\N	\N	MAGAZINE	DEMO	JOB003	3
3	QUEUED	\N	\N	\N	192.168.1.101:RIGHT	192.168.1.103:LEFT	\N	\N	2026-03-24 16:13:18.772963+00	2026-03-24 16:13:18.772963+00	\N	\N	\N	\N	\N	\N	\N	\N	\N	ACS01	\N	\N	LOAD	\N	\N	\N	MAGAZINE	DEMO	JOB004	4
\.


--
-- Data for Name: NA_U_COMMAND; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_U_COMMAND" ("ID", "MESSAGENAME", "APPLICATIONNAME", "APPLICATIONTYPE", "USERID", "CAUSE", "DESCRIPTION", "TIME", "CreateTime", "EditTime", "Creator", "Editor", "Name") FROM stdin;
\.


--
-- Data for Name: NA_U_INFORM; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_U_INFORM" (id, "time", type, message, source, description) FROM stdin;
\.


--
-- Data for Name: NA_U_TRANSPORT; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_U_TRANSPORT" ("ID", "MESSAGENAME", "TRANSPORTCOMMANDID", "SOURCEPORTID", "DESTPORTID", "VEHICLEID", "DESTNODEID", "REQUESTID", "USERID", "CAUSE", "DESCRIPTION", "TIME") FROM stdin;
\.


--
-- Data for Name: NA_X_APPLICATION; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_X_APPLICATION" (id, type, state, "startTime", "checkTime", "initialHardware", "runningHardware", "runningHardwareAddress", msb, memory, jmx, "destinationName", description, "createTime", "editTime", creator, editor, "NAME") FROM stdin;
CS01_P	control	active	2026-03-16 18:09:20.521467+00	2026-03-16 18:13:47.573787+00	\N	PRIMARY	127.0.0.1	rabbitmq	\N	\N	VM/DEMO/CONTROL/AGENT/	\N	2026-03-16 16:17:57.335547+00	2026-03-16 18:09:20.521467+00	admin	admin	CS01_P
HS01_P	host	active	2026-03-24 16:11:23.436075+00	2026-03-24 16:11:23.436075+00	\N	PRIMARY	127.0.0.1	rabbitmq	\N	\N	/HQ/NMG/ACS/HOST/LISTENER	\N	2026-03-14 17:26:30.265146+00	2026-03-24 16:11:23.436075+00	admin	admin	HS01_P
US01_P	ui	inactive	2026-03-24 14:00:14.726706+00	2026-03-24 14:00:14.726706+00	\N	PRIMARY	127.0.0.1	rabbitmq	\N	\N	/HQ/NMG/ACS/HOST/LISTENER	\N	2026-03-17 14:38:02.713545+00	2026-03-24 14:00:14.726706+00	admin	admin	US01_P
DS01_P	daemon	active	2026-03-24 16:16:38.306941+00	2026-03-24 16:16:38.306941+00	\N	PRIMARY	127.0.0.1	rabbitmq	\N	\N	VM/DEMO/DAEMON/LISTENER	\N	2026-03-20 07:18:09.138209+00	2026-03-24 16:16:38.306941+00	admin	admin	DS01_P
TS01_P	trans	active	2026-03-24 16:34:05.73855+00	2026-03-24 16:34:05.73855+00	\N	PRIMARY	127.0.0.1	rabbitmq	\N	\N	/HQ/NMG/ACS/HOST/LISTENER	\N	2026-03-19 11:16:51.888541+00	2026-03-24 16:34:05.73855+00	admin	admin	TS01_P
ES01_P	ei	active	2026-03-25 15:25:00.956831+00	2026-03-25 15:25:00.956831+00	\N	PRIMARY	127.0.0.1	rabbitmq	\N	\N	VM/DEMO/ES/	\N	2026-03-25 04:44:42.922612+00	2026-03-25 15:25:00.956831+00	admin	admin	ES01_P
TS02_P	trans	inactive	2026-03-19 11:37:23.738371+00	2026-03-19 11:37:23.738371+00	\N	PRIMARY	127.0.0.1	rabbitmq	\N	\N	/BN1/BNE3/ACS/FACTORY1/ACSmgr_2F	\N	2026-03-19 11:21:29.334409+00	2026-03-19 11:37:23.738371+00	admin	admin	TS02_P
UI01_P	ui	active	2026-03-16 18:08:42.689824+00	2026-03-16 18:13:30.576578+00	\N	PRIMARY	127.0.0.1	rabbitmq	\N	\N	/BN1/BNE3/ACS/FACTORY1/ACSmgr_2F	\N	2026-03-16 09:34:24.156878+00	2026-03-16 18:08:42.689824+00	admin	admin	UI01_P
\.


--
-- Data for Name: NA_X_APPLICATION_MANAGER; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_X_APPLICATION_MANAGER" ("ID", "TYPE", "COMMAND", "REPLY", "STATE", "USERID", "IPADDRESS", "EVENTTIME", "REQUESTTIME") FROM stdin;
\.


--
-- Data for Name: NA_X_OPTION; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NA_X_OPTION" (id, name, "nameDescription", value, "valueDescription", "subValue", "subValueDescription", used) FROM stdin;
1001	1001	USEBALANCELOAD	01	USED	\N	\N	T
1002	1002	USEDYNAMICLOAD	01	NOTUSED	\N	\N	T
1003	1003	USEHEURISTICDELAY	02	NOTUSED	\N	\N	T
1004	1004	USEBIDIRECTIONALNODE	01	USED	\N	\N	T
1102	1102	RULEFORSUITABLEMACHINEINGROUP	01	FULLRATE	\N	\N	T
1103	1103	USEINTERNALROUTE	02	NOTUSED	\N	\N	T
2001	2001	FORETRANSFERWHENDESTUNAVAILABLE	01	TRUE	\N	\N	T
2002	2002	USETRANSPORTFAILWHENFIRSTTRANSPORT	02	FALSE	\N	\N	T
2003	2003	CONSIDERALTERNATEDJOBWHENTRANSFERRING	01	USED	\N	\N	T
2005	2005	CONSIDERALTERNATEDJOBWHENTRANSFERRING	01	USED	-1	TIME	T
2006	2006	EXECUTEBATCHJOBINDUEORDER	01	TRUE	\N	\N	T
2007	2007	AWAKELIMITCOUNTFORALTERNATEDJOB	0	NOTUSED	\N	\N	T
2008	2008	FORETRANSFERWHENAWAKE	01	NOTUSED	\N	\N	T
3001	3001	SELECTTOBYSTOCKER	01	TRUE	\N	\N	F
3002	3002	USESTAGECOMMAND	02	NOTUSED	\N	\N	T
3003	3003	USESCANCOMMAND	02	NOTUSED	\N	\N	T
3004	3004	USEPRIORITYCHANGECOMMAND	02	NOTUSED	\N	\N	T
3005	3005	USEPORTINOUTTYPECHANGECOMMAND	02	NOTUSED	\N	\N	T
3006	3006	USEUPDATECOMMAND	02	NOTUSED	\N	\N	T
3007	3007	USEPERMITTEDCAPACITY	02	NOTUSED	\N	\N	T
3101	3101	USEPORTINCREASINGPRIORITY	02	NOTUSED	\N	\N	T
3102	3102	USESCANCOMMANDFORGARBAGECARRIER	02	NOTUSED	\N	\N	T
3103	3103	USEPRIORITYBOOSTUP	02	NOTUSED	\N	\N	T
4001	4001	USECARRIERPROCESSTYPE	01	USED	\N	\N	T
5101	5101	CONTROLMULTICRANEBYMCS	02	FALSE	\N	\N	F
5102	5102	GARBAGECARRIERCONTROLRULEFORSTORAGE	01	APPLICATIONSELECT	\N	\N	T
5106	5106	UNKNOWNCARRIERCONTROLRULEFORSTORAGE	01	APPLICATIONSELECT	\N	\N	T
5105	5105	TRANSPORTRULEWHENPHYSICALFULL	01	TOALTERNATESTORAGE	\N	\N	T
5201	5201	CHECKPROCESSMACHINETRANSFERSTATE	01	TRUE	\N	\N	T
5301	5301	CHECKRAILSTORAGEMACHINETRANSFERSTATE	01	TRUE	\N	\N	T
5206	5206	USEWAITONPROCESSMACHINEFORUNLOADING	02	NOTUSED	\N	\N	T
5109	5109	INSTALLCOMMANDWHENENHANCEDCARRIERLISTZERO	01	USED	\N	\N	T
5302	5302	CHECKRAILSTORAGEMACHINEPORTOCCUPIED	01	TRUE	\N	\N	T
6101	6101	CHECKPROCESSMACHINEPORTOCCUPIED	01	TRUE	\N	\N	T
6102	6102	CHECKPROCESSMACHINEPORTSUBSTATE	01	TRUE	\N	\N	T
6103	6103	USERECOVERYTRANSPORTONPROCESSMACHINEPORT	02	NOTUSED	\N	\N	T
6104	6104	USECHANGEPREVIOUSCARRIERLOCATIONTONOTAPPLICABLEONPORT	02	NOTUSED	\N	\N	T
7001	7001	ACCEPTTRANSPORTREQUEST	01	TRUE	\N	\N	T
7002	7002	PERMITMATERIALMOVEMENT	01	TRUE	\N	\N	T
7003	7003	ACCEPTDESTCHANGEREQUEST	01	TRUE	\N	\N	T
\.


--
-- Name: NA_R_BAY_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."NA_R_BAY_id_seq"', 1, true);


--
-- Name: NA_R_LOCATION_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."NA_R_LOCATION_id_seq"', 6, true);


--
-- Name: NA_R_VEHICLE_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."NA_R_VEHICLE_id_seq"', 1, true);


--
-- Name: NA_R_ZONE_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."NA_R_ZONE_id_seq"', 1, true);


--
-- Name: NA_T_TRANSPORTCMD_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."NA_T_TRANSPORTCMD_id_seq"', 4, true);


--
-- Name: NA_C_MQTT NA_C_MQTT_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_C_MQTT"
    ADD CONSTRAINT "NA_C_MQTT_pkey" PRIMARY KEY (id);

ALTER TABLE ONLY public."NA_C_MQTT"
    ADD CONSTRAINT "NA_C_MQTT_NAME_unique" UNIQUE ("NAME");


--
-- Name: NA_R_BAY NA_R_BAY_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_R_BAY"
    ADD CONSTRAINT "NA_R_BAY_pkey" PRIMARY KEY (id);


--
-- Name: NA_R_LOCATION NA_R_LOCATION_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_R_LOCATION"
    ADD CONSTRAINT "NA_R_LOCATION_pkey" PRIMARY KEY (id);


--
-- Name: NA_R_VEHICLE NA_R_VEHICLE_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_R_VEHICLE"
    ADD CONSTRAINT "NA_R_VEHICLE_pkey" PRIMARY KEY (id);


--
-- Name: NA_R_ZONE NA_R_ZONE_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_R_ZONE"
    ADD CONSTRAINT "NA_R_ZONE_pkey" PRIMARY KEY (id);


--
-- Name: NA_T_TRANSPORTCMD NA_T_TRANSPORTCMD_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_T_TRANSPORTCMD"
    ADD CONSTRAINT "NA_T_TRANSPORTCMD_pkey" PRIMARY KEY (id);


--
-- Name: NA_A_ALARM PK_NA_A_ALARM; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_A_ALARM"
    ADD CONSTRAINT "PK_NA_A_ALARM" PRIMARY KEY (id);


--
-- Name: NA_A_ALARMSPEC PK_NA_A_ALARMSPEC; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_A_ALARMSPEC"
    ADD CONSTRAINT "PK_NA_A_ALARMSPEC" PRIMARY KEY (id);


--
-- Name: NA_C_NIO PK_NA_C_NIO; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_C_NIO"
    ADD CONSTRAINT "PK_NA_C_NIO" PRIMARY KEY (id);


--
-- Name: NA_H_ALARMRPTHISTORY PK_NA_H_ALARMRPTHISTORY; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_H_ALARMRPTHISTORY"
    ADD CONSTRAINT "PK_NA_H_ALARMRPTHISTORY" PRIMARY KEY (id);


--
-- Name: NA_H_ALARMTIMEHISTORY PK_NA_H_ALARMTIMEHISTORY; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_H_ALARMTIMEHISTORY"
    ADD CONSTRAINT "PK_NA_H_ALARMTIMEHISTORY" PRIMARY KEY (id);


--
-- Name: NA_H_CROSSWAIT_HISTORY PK_NA_H_CROSSWAIT_HISTORY; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_H_CROSSWAIT_HISTORY"
    ADD CONSTRAINT "PK_NA_H_CROSSWAIT_HISTORY" PRIMARY KEY (id);


--
-- Name: NA_H_HEARTBEATFAILHISTORY PK_NA_H_HEARTBEATFAILHISTORY; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_H_HEARTBEATFAILHISTORY"
    ADD CONSTRAINT "PK_NA_H_HEARTBEATFAILHISTORY" PRIMARY KEY (id);


--
-- Name: NA_H_MISSMATCHANDFLYHISTORY PK_NA_H_MISSMATCHANDFLYHISTORY; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_H_MISSMATCHANDFLYHISTORY"
    ADD CONSTRAINT "PK_NA_H_MISSMATCHANDFLYHISTORY" PRIMARY KEY (id);


--
-- Name: NA_H_NIOHISTORY PK_NA_H_NIOHISTORY; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_H_NIOHISTORY"
    ADD CONSTRAINT "PK_NA_H_NIOHISTORY" PRIMARY KEY (id);


--
-- Name: NA_H_TRANSPORTCMDHISTORY PK_NA_H_TRANSPORTCMDHISTORY; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_H_TRANSPORTCMDHISTORY"
    ADD CONSTRAINT "PK_NA_H_TRANSPORTCMDHISTORY" PRIMARY KEY (id);


--
-- Name: NA_H_VEHICLEHISTORY PK_NA_H_VEHICLEHISTORY; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_H_VEHICLEHISTORY"
    ADD CONSTRAINT "PK_NA_H_VEHICLEHISTORY" PRIMARY KEY (id);


--
-- Name: NA_H_VEHICLESEARCHPATH PK_NA_H_VEHICLESEARCHPATH; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_H_VEHICLESEARCHPATH"
    ADD CONSTRAINT "PK_NA_H_VEHICLESEARCHPATH" PRIMARY KEY (id);


--
-- Name: NA_H_VEHICLE_BATTERYHISTORY PK_NA_H_VEHICLE_BATTERYHISTORY; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_H_VEHICLE_BATTERYHISTORY"
    ADD CONSTRAINT "PK_NA_H_VEHICLE_BATTERYHISTORY" PRIMARY KEY (id);


--
-- Name: NA_L_LARGELOGMESSAGE PK_NA_L_LARGELOGMESSAGE; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_L_LARGELOGMESSAGE"
    ADD CONSTRAINT "PK_NA_L_LARGELOGMESSAGE" PRIMARY KEY (id);


--
-- Name: NA_L_LOGMESSAGE PK_NA_L_LOGMESSAGE; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_L_LOGMESSAGE"
    ADD CONSTRAINT "PK_NA_L_LOGMESSAGE" PRIMARY KEY (id);


--
-- Name: NA_M_CARRIER PK_NA_M_CARRIER; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_M_CARRIER"
    ADD CONSTRAINT "PK_NA_M_CARRIER" PRIMARY KEY (id);


--
-- Name: NA_Q_TRANSPORTCMDREQUEST PK_NA_Q_TRANSPORTCMDREQUEST; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_Q_TRANSPORTCMDREQUEST"
    ADD CONSTRAINT "PK_NA_Q_TRANSPORTCMDREQUEST" PRIMARY KEY (id);


--
-- Name: NA_R_LINK PK_NA_R_LINK; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_R_LINK"
    ADD CONSTRAINT "PK_NA_R_LINK" PRIMARY KEY (id);


--
-- Name: NA_R_LINK_ZONE PK_NA_R_LINK_ZONE; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_R_LINK_ZONE"
    ADD CONSTRAINT "PK_NA_R_LINK_ZONE" PRIMARY KEY (id);


--
-- Name: NA_R_NODE PK_NA_R_NODE; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_R_NODE"
    ADD CONSTRAINT "PK_NA_R_NODE" PRIMARY KEY (id);


--
-- Name: NA_R_NODE UQ_NA_R_NODE_node_id; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_R_NODE"
    ADD CONSTRAINT "UQ_NA_R_NODE_node_id" UNIQUE (node_id);


--
-- Name: NA_R_ORDER_PAIR PK_NA_R_ORDER_PAIR; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_R_ORDER_PAIR"
    ADD CONSTRAINT "PK_NA_R_ORDER_PAIR" PRIMARY KEY (id);


--
-- Name: NA_R_SPECIALCONFIG PK_NA_R_SPECIALCONFIG; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_R_SPECIALCONFIG"
    ADD CONSTRAINT "PK_NA_R_SPECIALCONFIG" PRIMARY KEY ("ID");


--
-- Name: NA_R_STATION PK_NA_R_STATION; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_R_STATION"
    ADD CONSTRAINT "PK_NA_R_STATION" PRIMARY KEY (id);


--
-- Name: NA_R_VEHICLE_CROSS_WAIT PK_NA_R_VEHICLE_CROSS_WAIT; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_R_VEHICLE_CROSS_WAIT"
    ADD CONSTRAINT "PK_NA_R_VEHICLE_CROSS_WAIT" PRIMARY KEY ("vehicleId");


--
-- Name: NA_R_VEHICLE_IDLE PK_NA_R_VEHICLE_IDLE; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_R_VEHICLE_IDLE"
    ADD CONSTRAINT "PK_NA_R_VEHICLE_IDLE" PRIMARY KEY (id);


--
-- Name: NA_R_VEHICLE_ORDER PK_NA_R_VEHICLE_ORDER; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_R_VEHICLE_ORDER"
    ADD CONSTRAINT "PK_NA_R_VEHICLE_ORDER" PRIMARY KEY (id);


--
-- Name: NA_T_CURRENTINTERSECTION PK_NA_T_CURRENTINTERSECTION; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_T_CURRENTINTERSECTION"
    ADD CONSTRAINT "PK_NA_T_CURRENTINTERSECTION" PRIMARY KEY (id);


--
-- Name: NA_T_INTERSECTION PK_NA_T_INTERSECTION; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_T_INTERSECTION"
    ADD CONSTRAINT "PK_NA_T_INTERSECTION" PRIMARY KEY (id);


--
-- Name: NA_U_COMMAND PK_NA_U_COMMAND; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_U_COMMAND"
    ADD CONSTRAINT "PK_NA_U_COMMAND" PRIMARY KEY ("ID");


--
-- Name: NA_U_INFORM PK_NA_U_INFORM; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_U_INFORM"
    ADD CONSTRAINT "PK_NA_U_INFORM" PRIMARY KEY (id);


--
-- Name: NA_U_TRANSPORT PK_NA_U_TRANSPORT; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_U_TRANSPORT"
    ADD CONSTRAINT "PK_NA_U_TRANSPORT" PRIMARY KEY ("ID");


--
-- Name: NA_X_APPLICATION PK_NA_X_APPLICATION; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_X_APPLICATION"
    ADD CONSTRAINT "PK_NA_X_APPLICATION" PRIMARY KEY (id);


--
-- Name: NA_X_APPLICATION_MANAGER PK_NA_X_APPLICATION_MANAGER; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_X_APPLICATION_MANAGER"
    ADD CONSTRAINT "PK_NA_X_APPLICATION_MANAGER" PRIMARY KEY ("ID");


--
-- Name: NA_X_OPTION PK_NA_X_OPTION; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_X_OPTION"
    ADD CONSTRAINT "PK_NA_X_OPTION" PRIMARY KEY (id);


--
-- Name: NA_R_LOCATION uq_location_locationId; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NA_R_LOCATION"
    ADD CONSTRAINT "uq_location_locationId" UNIQUE ("locationId");


--
-- Views for EF Core entity mappings
--

-- NA_R_LINK_VW: Link + LinkZone + Zone + Node 좌표 결합
CREATE OR REPLACE VIEW public."NA_R_LINK_VW" AS
SELECT
    lz.id,
    lz."zoneId",
    lz."transferFlag",
    z."bayId",
    l."fromNodeId",
    fn.xpos::int AS from_xpos,
    fn.ypos::int AS from_ypos,
    l."toNodeId",
    tn.xpos::int AS to_xpos,
    tn.ypos::int AS to_ypos,
    l.length,
    l.speed,
    l."leftBranch",
    l.availability,
    l.load,
    0 AS loading
FROM public."NA_R_LINK_ZONE" lz
JOIN public."NA_R_LINK" l ON l.id = lz."linkId"
JOIN public."NA_R_ZONE" z ON z."zoneId" = lz."zoneId"
JOIN public."NA_R_NODE" fn ON fn.node_id = l."fromNodeId"
JOIN public."NA_R_NODE" tn ON tn.node_id = l."toNodeId";

-- NA_R_LOCATION_VW: Location + Station + LinkZone + Zone + Link + Node 좌표 결합
CREATE OR REPLACE VIEW public."NA_R_LOCATION_VW" AS
SELECT
    loc."locationId" AS "portId",
    s.id AS "stationId",
    z."bayId",
    loc.type AS "location_Type",
    loc."carrierType",
    loc.direction,
    loc.state,
    lz.id AS "linkId",
    l."fromNodeId" AS "parentNode",
    s.type AS "station_type",
    s.distance,
    fn.xpos::int AS from_xpos,
    fn.ypos::int AS from_ypos,
    tn.xpos::int AS to_xpos,
    tn.ypos::int AS to_ypos,
    l.length,
    l."leftBranch",
    l.availability,
    l.load,
    lz."transferFlag"
FROM public."NA_R_LOCATION" loc
JOIN public."NA_R_STATION" s ON s.id = loc."stationId"
JOIN public."NA_R_LINK_ZONE" lz ON lz.id = s."linkId"
JOIN public."NA_R_LINK" l ON l.id = lz."linkId"
JOIN public."NA_R_ZONE" z ON z."zoneId" = lz."zoneId"
JOIN public."NA_R_NODE" fn ON fn.node_id = l."fromNodeId"
JOIN public."NA_R_NODE" tn ON tn.node_id = l."toNodeId";

-- NA_R_STATION_VW: Station + Link의 fromNode/toNode 결합
CREATE OR REPLACE VIEW public."NA_R_STATION_VW" AS
SELECT
    s.id,
    s."linkId",
    l."fromNodeId" AS "parentNode",
    l."toNodeId" AS "nextNode",
    s.type,
    s.distance
FROM public."NA_R_STATION" s
JOIN public."NA_R_LINK_ZONE" lz ON lz.id = s."linkId"
JOIN public."NA_R_LINK" l ON l.id = lz."linkId";

-- NA_R_WAITP_VW: Node(WAIT_P 타입) + LinkZone에서 zoneId 결합
CREATE OR REPLACE VIEW public."NA_R_WAITP_VW" AS
SELECT
    n.node_id AS id,
    n.type,
    lz."zoneId"
FROM public."NA_R_NODE" n
JOIN public."NA_R_LINK" l ON l."fromNodeId" = n.node_id OR l."toNodeId" = n.node_id
JOIN public."NA_R_LINK_ZONE" lz ON lz."linkId" = l.id
WHERE n.type IN ('WAIT_P', 'S_WAIT_P', 'A_WAIT_P', 'B_WAIT_P')
GROUP BY n.node_id, n.type, lz."zoneId";

--
-- PostgreSQL database dump complete
--

\unrestrict 9iT4XZGDbWKs4149DiOO9dfg9GR0kbPjMsGKFNG4m6wJWNs9Jo8AsiOmaoFDpTb

