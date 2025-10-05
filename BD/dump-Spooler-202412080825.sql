--
-- PostgreSQL database cluster dump
--

-- Started on 2024-12-08 08:25:05

SET default_transaction_read_only = off;

SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;

--
-- Roles
--

CREATE ROLE postgres;
ALTER ROLE postgres WITH SUPERUSER INHERIT CREATEROLE CREATEDB LOGIN REPLICATION BYPASSRLS;

--
-- User Configurations
--








--
-- Databases
--

--
-- Database "template1" dump
--

\connect template1

--
-- PostgreSQL database dump
--

-- Dumped from database version 16.3
-- Dumped by pg_dump version 16.3

-- Started on 2024-12-08 08:25:05

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

-- Completed on 2024-12-08 08:25:05

--
-- PostgreSQL database dump complete
--

--
-- Database "Spooler" dump
--

--
-- PostgreSQL database dump
--

-- Dumped from database version 16.3
-- Dumped by pg_dump version 16.3

-- Started on 2024-12-08 08:25:05

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

--
-- TOC entry 4793 (class 1262 OID 16398)
-- Name: Spooler; Type: DATABASE; Schema: -; Owner: postgres
--

CREATE DATABASE "Spooler" WITH TEMPLATE = template0 ENCODING = 'UTF8' LOCALE_PROVIDER = libc LOCALE = 'Spanish_Venezuela.1252';


ALTER DATABASE "Spooler" OWNER TO postgres;

\connect "Spooler"

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

--
-- TOC entry 218 (class 1255 OID 16414)
-- Name: getlastprinteddocument(character varying, character varying); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.getlastprinteddocument(v_printer_serial character varying, v_document_type character varying) RETURNS json
    LANGUAGE plpgsql
    AS $$
		declare document_json json;
	
	BEGIN
	
		select jsonb_agg(t) into document_json
		from(
		SELECT c."id"::int as id,
		c."document_reference"::text AS document_reference,
		coalesce(c."document_number",'0')::integer AS document_number
		FROM public."line" c
		WHERE upper(c."printer_serial")=upper(v_printer_serial)
		and upper(c."document_type")=upper(v_document_type)
		and c."printed" = true 
		and c."reprint" = false  
		ORDER BY coalesce(c."document_number",'0')::integer desc
		limit 1
		
		) t;
	
		return coalesce(document_json,'{}') as document ;
		
	END;
	$$;


ALTER FUNCTION public.getlastprinteddocument(v_printer_serial character varying, v_document_type character varying) OWNER TO postgres;

--
-- TOC entry 232 (class 1255 OID 16408)
-- Name: getline(character varying); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.getline(v_printer_serial character varying) RETURNS TABLE(id integer, document_reference character varying, printer_serial character varying, document_json text, printed_note character varying, document_number character varying, document_type integer, reprint boolean)
    LANGUAGE plpgsql
    AS $$
 
BEGIN

	RETURN QUERY
	SELECT c."id",
	c."document_reference" AS document_reference,
	c."printer_serial" AS printer_serial,
	c."document_json" AS document_json,
	coalesce(c."printed_note",'') AS printed_note, 
	coalesce(c."document_number",'') AS document_number,
	case when c."document_type" = 'F' then 1 
		 when c."document_type" = 'N' then 2
		 when c."document_type" = 'X' then 3 
		 when c."document_type" = 'Z' then 4 
		 when c."document_type" = 'P' then 5
		 else 6 end AS document_type,
	c."reprint"
	FROM public."line" c
	WHERE upper(c."printer_serial")=upper(v_printer_serial)
	and c."printed" = false 
	ORDER BY c."id";

END;
$$;


ALTER FUNCTION public.getline(v_printer_serial character varying) OWNER TO postgres;

--
-- TOC entry 231 (class 1255 OID 16413)
-- Name: getlinejson(character varying); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.getlinejson(v_printer_serial character varying) RETURNS json
    LANGUAGE plpgsql
    AS $$
		declare document_json json;
	
	BEGIN
	
		select jsonb_agg(t) into document_json
		from(
		SELECT c."id"::int as id,
		c."document_reference"::text AS document_reference,
		c."printer_serial" AS printer_serial,
	        c."document_json" AS document_json,
		coalesce(c."printed_note",'') AS printed_note, 
		coalesce(c."document_number",'') AS document_number,
		case when c."document_type" = 'F' then 1 
			 when c."document_type" = 'N' then 2
			 when c."document_type" = 'X' then 3 
			 when c."document_type" = 'Z' then 4 
			 when c."document_type" = 'P' then 5
		 	 else 6 end AS document_type,
		c."reprint"
		FROM public."line" c
		WHERE upper(c."printer_serial")=upper(v_printer_serial)
		and c."printed" = false 
		ORDER BY c."id"
		) t;
	
		return coalesce(document_json,'{}') as document_status ;
		
	END;
	$$;


ALTER FUNCTION public.getlinejson(v_printer_serial character varying) OWNER TO postgres;

--
-- TOC entry 217 (class 1255 OID 16409)
-- Name: getstatus(character varying, character varying, character varying); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.getstatus(v_document_reference character varying, v_printer_serial character varying, v_document_type character varying) RETURNS json
    LANGUAGE plpgsql
    AS $$
	declare document_json json;

BEGIN

	select jsonb_agg(t) into document_json
	from(
	SELECT c."id",
	c."document_reference" AS document_reference,
	c."printer_serial" AS printer_serial,
	coalesce(c."printed_note",'') AS printed_note, 
	coalesce(c."printed_date",'') AS printed_date,
	coalesce(c."document_number",'') AS document_number,
	c."document_type" AS document_type
	FROM public."line" c
	WHERE 
	upper(c."printer_serial")=upper(v_printer_serial)
	and upper(c."document_reference")=upper(v_document_reference)
	and upper(c."document_type")=upper(v_document_type)
	) t;

	return document_json as document_status ;
	
END;
$$;


ALTER FUNCTION public.getstatus(v_document_reference character varying, v_printer_serial character varying, v_document_type character varying) OWNER TO postgres;

--
-- TOC entry 233 (class 1255 OID 16410)
-- Name: insertline(character varying, character varying, text, character varying, boolean); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.insertline(v_document_reference character varying, v_printer_serial character varying, t_document_json text, v_document_type character varying, b_reprint boolean) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
 
BEGIN

	PERFORM set_config('timezone', 'America/New_York', true);

	IF NOT EXISTS (
				   SELECT * FROM PUBLIC."line"
				   WHERE upper("document_reference")=upper(v_document_reference)
				   and upper("printer_serial")=upper(v_printer_serial)
				   and upper("document_type")=upper(v_document_type)
				   ) THEN
		INSERT INTO PUBLIC."line" ("document_reference","printer_serial","document_json","document_type","created_date","reprint")
		VALUES (v_document_reference,v_printer_serial,t_document_json,v_document_type,CURRENT_TIMESTAMP,b_reprint);
	ELSE
		UPDATE PUBLIC."line"
			SET "printer_serial"=v_printer_serial,"document_json"=t_document_json,"document_type"=v_document_type,"created_date"=CURRENT_TIMESTAMP
		WHERE upper("document_reference")=upper(v_document_reference)
		and upper("printer_serial")=upper(v_printer_serial)
		and upper("document_type")=upper(v_document_type)
		and "printed"= false ;
		
		IF upper(v_document_type)='P' then
			UPDATE PUBLIC."line"
			SET "printer_serial"=v_printer_serial,"document_json"=t_document_json,"document_type"=v_document_type,"created_date"=CURRENT_TIMESTAMP,"printed"=false 
			WHERE upper("document_reference")=upper(v_document_reference)
			and upper("printer_serial")=upper(v_printer_serial)
			and upper("document_type")=upper(v_document_type);	
		END IF;
		
	END IF;
	
	RETURN true;
END;
$$;


ALTER FUNCTION public.insertline(v_document_reference character varying, v_printer_serial character varying, t_document_json text, v_document_type character varying, b_reprint boolean) OWNER TO postgres;

--
-- TOC entry 219 (class 1255 OID 16411)
-- Name: updateline(integer, character varying, character varying); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.updateline(i_id integer, v_document_number character varying, v_printed_note character varying) RETURNS boolean
    LANGUAGE plpgsql
    AS $$

		declare doctype varchar;
		declare breprint bool := false;

	begin
		
		PERFORM set_config('timezone', 'America/New_York', true);

		select l.document_type, l.reprint into doctype, breprint from PUBLIC."line" l where l.id=i_id;
		
		IF NOT exists (select * from line where document_number=v_document_number and document_type=doctype) 
			or breprint=true or doctype<>'F' then
			UPDATE PUBLIC."line" l
				SET "document_number"=v_document_number,
				"printed_note"=v_printed_note,
				"printed"=true,
				"printed_date"=clock_timestamp()::varchar(19)
			WHERE l."id"=i_id and "printed"=false;
		END IF;

		return true;

	END;
$$;


ALTER FUNCTION public.updateline(i_id integer, v_document_number character varying, v_printed_note character varying) OWNER TO postgres;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- TOC entry 216 (class 1259 OID 16400)
-- Name: line; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.line (
    id integer NOT NULL,
    document_reference character varying(10),
    document_type character varying(1),
    document_number character varying(20),
    document_json text,
    printer_serial character varying(20),
    printed_note character varying(100),
    printed_date character varying(20),
    printed boolean DEFAULT false NOT NULL,
    created_date timestamp without time zone,
    reprint boolean
);


ALTER TABLE public.line OWNER TO postgres;

--
-- TOC entry 215 (class 1259 OID 16399)
-- Name: line_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public.line ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.line_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- TOC entry 4787 (class 0 OID 16400)
-- Dependencies: 216
-- Data for Name: line; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.line (id, document_reference, document_type, document_number, document_json, printer_serial, printed_note, printed_date, printed, created_date, reprint) FROM stdin;
3	ref003	F		{"Reference": "ref005", "Printer_Serial": "ser001", "Client": {"Vat": "16442063", "Name": "Wilman Camacho", "Address": "bqto"}, "Products": [{"Code": "01", "Name": "producto 1", "Quantity": 1, "Price": 1.45, "Tax": "16"}], "Payments": [{"Code": "01", "Name": "efectivo", "Amount": 1.45}], "Note": "", "Number": "", "Date": "", "Type": "F"}	Z7C7007088		2024-07-30 14:37:09	t	2024-07-28 22:15:46.794778	f
4	ref004	F		{"Reference": "ref005", "Printer_Serial": "ser001", "Client": {"Vat": "16442063", "Name": "Wilman Camacho", "Address": "bqto"}, "Products": [{"Code": "01", "Name": "producto 1", "Quantity": 1, "Price": 1.45, "Tax": "16"}], "Payments": [{"Code": "01", "Name": "efectivo", "Amount": 1.45}], "Note": "", "Number": "", "Date": "", "Type": "F"}	Z7C7007088		2024-07-30 14:37:09	t	2024-07-28 22:15:50.891896	f
7	ref006	N	835	{"Reference": "ref006", "Printer_Serial": "Z7C7007088", "Client": {"Vat": "16442063", "Name": "Wilman Camacho", "Address": "Calle 6"}, "Products": [{"Code": "01", "Name": "Acetaminofen", "Quantity": 1, "Price": 1.45, "Tax": "G"}], "Payments": [{"Code": "01", "Name": "efectivo", "Amount": 1.68}], "Note": "", "Number": "84782", "Date": "2024-08-10 10:05:28", "Type": "N"}	Z7C7007088		2024-08-10 12:06:37	t	2024-08-10 11:03:44.713459	f
5	ref005	F		{"Reference": "ref005", "Printer_Serial": "ser001", "Client": {"Vat": "16442063", "Name": "Wilman Camacho", "Address": "bqto"}, "Products": [{"Code": "01", "Name": "producto 1", "Quantity": 1, "Price": 1.45, "Tax": "16"}], "Payments": [{"Code": "01", "Name": "efectivo", "Amount": 1.45}], "Note": "", "Number": "", "Date": "", "Type": "F"}	Z7C7007088		2024-07-30 14:37:00	t	2024-07-28 22:15:55.895204	f
12	REF0011	N	\N	{"Reference": "REF0011", "Printer_Serial": "Z7C7007088", "Type": "N", "Number": "836"}	Z7C7007088	\N	\N	t	2024-08-10 15:36:12.637154	f
8	ref008	N	836	{"Reference": "ref008", "Printer_Serial": "Z7C7007088", "Client": {"Vat": "16442063", "Name": "Wilman Camacho", "Address": "Calle 6"}, "Products": [{"Code": "01", "Name": "Acetaminofen", "Quantity": 1, "Price": 1.45, "Tax": "G"}], "Payments": [{"Code": "01", "Name": "efectivo", "Amount": 1.68}], "Note": "", "Number": "84783", "Date": "2024-08-10", "Type": "N"}	Z7C7007088		2024-08-10 12:58:26	t	2024-08-10 12:56:59.161024	f
2	ref002	X		{"Reference": "ref005", "Printer_Serial": "ser001", "Client": {"Vat": "16442063", "Name": "Wilman Camacho", "Address": "bqto"}, "Products": [{"Code": "01", "Name": "producto 1", "Quantity": 1, "Price": 1.45, "Tax": "16"}], "Payments": [{"Code": "01", "Name": "efectivo", "Amount": 1.45}], "Note": "", "Number": "", "Date": "", "Type": "F"}	Z7C7007088	P	2024-08-13 23:40:11	t	2024-07-28 22:15:41.684097	f
16	REF0016	F	84784	{"Reference": "REF0016", "Printer_Serial": "Z7C7007088", "Type": "F", "Number": "84784"}	Z7C7007088		2024-08-10 16:53:58	t	2024-08-10 15:56:10.453228	t
17	REF0017	Z	892	{"Reference": "REF0017", "Printer_Serial": "Z7C7007088", "Type": "Z"}	Z7C7007088	P	2024-08-17 18:41:29	t	2024-08-10 16:08:17.353405	f
9	REF009	F	84784	{"Reference": "REF009", "Printer_Serial": "Z7C7007088", "Client": {"Vat": "V16442063", "Name": "WILMAN JOSE CAMACHO GONZALEZ 123456789 ABCDEFGH", "Address": "SANTA ISABEL SAN FRANCISCO CALLE 06 CARR 5 OESTE"}, "Products": [{"Code": "01", "Name": "ACETAMINOFEN+CAFEINA PASTILLA TABLETA 30MG ADULTO", "Quantity": 1, "Price": 1.45, "Tax": "G"}, {"Code": "02", "Name": "ACETAMINOFEN+CAFEINA PASTILLA TABLETA 15MG ADULTO", "Quantity": 1, "Price": 1.45, "Tax": "E"}], "Payments": [{"Code": "01", "Name": "efectivo", "Amount": 1.68}, {"Code": "02", "Name": "debito", "Amount": 1.45}], "Note": "", "Number": "", "Date": "", "Type": "F"}	Z7C7007088		2024-08-10 13:16:06	t	2024-08-10 13:10:48.76632	f
19	REF019	F	84786	{"Reference": "REF019", "Printer_Serial": "Z7C7007088", "Client": {"Vat": "V16442063", "Name": "WILMAN JOSE CAMACHO GONZALEZ 123456789 ABCDEFGH", "Address": "SANTA ISABEL SAN FRANCISCO CALLE 06 CARR 5 OESTE"}, "Products": [{"Code": "01", "Name": "ACETAMINOFEN+CAFEINA PASTILLA TABLETA 30MG ADULTO", "Quantity": 1, "Price": 1.45, "Tax": "G"}, {"Code": "02", "Name": "ACETAMINOFEN+CAFEINA PASTILLA TABLETA 15MG ADULTO", "Quantity": 1, "Price": 1.45, "Tax": "E"}], "Payments": [{"Code": "01", "Name": "efectivo", "Amount": 1.68}, {"Code": "02", "Name": "debito", "Amount": 1.45}], "Note": "", "Number": "", "Date": "", "Type": "F"}	Z7C7007088		2024-08-10 16:54:22	t	2024-08-10 16:52:31.514359	f
6	ref007	F	84783	{"Reference": "ref007", "Printer_Serial": "Z7C7007088", "Client": {"Vat": "16442063", "Name": "Wilman Camacho", "Address": "Calle 6"}, "Products": [{"Code": "01", "Name": "Acetaminofen", "Quantity": 1, "Price": 1.45, "Tax": "G"}], "Payments": [{"Code": "01", "Name": "efectivo", "Amount": 1.68}], "Note": "", "Number": "", "Date": "", "Type": "F"}	Z7C7007088		2024-08-10 12:53:46	t	2024-08-10 10:46:54.085809	f
13	ref12	X		{"Reference": "ref12", "Printer_Serial": "Z7C7007088", "Type": "X"}	Z7C7007088		2024-08-10 16:31:04	t	2024-08-10 15:39:26.726298	f
11	REF0010	F	84784	{"Reference": "REF0010", "Printer_Serial": "Z7C7007088", "Type": "F", "Number": "84784"}	Z7C7007088		2024-08-10 16:48:16	t	2024-08-10 15:32:20.974015	t
14	REF0014	F	84782	{"Reference": "REF0014", "Printer_Serial": "Z7C7007088", "Type": "F", "Number": "84782"}	Z7C7007088		2024-08-10 16:48:19	t	2024-08-10 15:55:55.891281	t
15	REF0015	F	84783	{"Reference": "REF0015", "Printer_Serial": "Z7C7007088", "Type": "F", "Number": "84783"}	Z7C7007088		2024-08-10 16:48:44	t	2024-08-10 15:56:03.402157	t
10	ref13	N	837	{"Reference": "REF009", "Printer_Serial": "Z7C7007088", "Client": {"Vat": "V16442063", "Name": "WILMAN JOSE CAMACHO GONZALEZ 123456789 ABCDEFGH", "Address": "SANTA ISABEL SAN FRANCISCO CALLE 06 CARR 5 OESTE"}, "Products": [{"Code": "01", "Name": "ACETAMINOFEN+CAFEINA PASTILLA TABLETA 30MG ADULTO", "Quantity": 1, "Price": 1.45, "Tax": "G"}, {"Code": "02", "Name": "ACETAMINOFEN+CAFEINA PASTILLA TABLETA 15MG ADULTO", "Quantity": 1, "Price": 1.45, "Tax": "E"}], "Payments": [{"Code": "01", "Name": "efectivo", "Amount": 1.68}, {"Code": "02", "Name": "debito", "Amount": 1.45}], "Note": "", "Number": "84784", "Date": "2024-08-10 13:16:06", "Type": "N"}	Z7C7007088		2024-08-10 13:18:00	t	2024-08-10 13:17:03.501721	f
1	ref001	F	84795	{\n  "Reference": "ref005",\n  "Printer_Serial": "ser001",\n  "Client": {\n    "Vat": "16442063",\n    "Name": "Wilman Camacho",\n    "Address": "bqto"\n  },\n  "Products": [\n    {\n      "Code": "01",\n      "Name": "producto 1",\n      "Quantity": 1,\n      "Price": 5.00,\n      "Tax": "G"\n    }\n  ],\n  "Payments": [\n    {\n      "Code": "01",\n      "Name": "efectivo",\n      "Amount": 5.8\n    }\n  ],\n  "Note": "",\n  "Number": "",\n  "Date": "",\n  "Type": "F"\n}	Z7C7007088		2024-08-10 18:49:01	t	2024-07-28 22:14:04.222341	f
22	REF022	F	84789	{"Reference": "REF022", "Printer_Serial": "Z7C7007088", "Client": {"Vat": "V16442063", "Name": "WILMAN JOSE CAMACHO GONZALEZ 123456789 ABCDEFGH", "Address": "SANTA ISABEL SAN FRANCISCO CALLE 06 CARR 5 OESTE"}, "Products": [{"Code": "01", "Name": "ACETAMINOFEN+CAFEINA PASTILLA TABLETA 30MG ADULTO", "Quantity": 1, "Price": 1.45, "Tax": "G"}, {"Code": "02", "Name": "ACETAMINOFEN+CAFEINA PASTILLA TABLETA 15MG ADULTO", "Quantity": 1, "Price": 1.45, "Tax": "E"}], "Payments": [{"Code": "01", "Name": "efectivo", "Amount": 1.68}, {"Code": "02", "Name": "debito", "Amount": 1.45}], "Note": "", "Number": "", "Date": "", "Type": "F"}	Z7C7007088		2024-08-10 16:56:00	t	2024-08-10 16:53:40.628729	f
23	REF023	N	838	{"Reference": "REF023", "Printer_Serial": "Z7C7007088", "Client": {"Vat": "V16442063", "Name": "WILMAN JOSE CAMACHO GONZALEZ 123456789 ABCDEFGH", "Address": "SANTA ISABEL SAN FRANCISCO CALLE 06 CARR 5 OESTE"}, "Products": [{"Code": "01", "Name": "ACETAMINOFEN+CAFEINA PASTILLA TABLETA 30MG ADULTO", "Quantity": 1, "Price": 1.45, "Tax": "G"}, {"Code": "02", "Name": "ACETAMINOFEN+CAFEINA PASTILLA TABLETA 15MG ADULTO", "Quantity": 1, "Price": 1.45, "Tax": "E"}], "Payments": [{"Code": "01", "Name": "efectivo", "Amount": 1.68}, {"Code": "02", "Name": "debito", "Amount": 1.45}], "Note": "", "Number": "84785", "Date": "2024-08-10", "Type": "N"}	Z7C7007088		2024-08-10 16:59:18	t	2024-08-10 16:57:41.577669	f
24	REF024	N	839	{"Reference": "REF024", "Printer_Serial": "Z7C7007088", "Client": {"Vat": "V16442063", "Name": "WILMAN JOSE CAMACHO GONZALEZ 123456789 ABCDEFGH", "Address": "SANTA ISABEL SAN FRANCISCO CALLE 06 CARR 5 OESTE"}, "Products": [{"Code": "01", "Name": "ACETAMINOFEN+CAFEINA PASTILLA TABLETA 30MG ADULTO", "Quantity": 1, "Price": 1.45, "Tax": "G"}, {"Code": "02", "Name": "ACETAMINOFEN+CAFEINA PASTILLA TABLETA 15MG ADULTO", "Quantity": 1, "Price": 1.45, "Tax": "E"}], "Payments": [{"Code": "01", "Name": "efectivo", "Amount": 1.68}, {"Code": "02", "Name": "debito", "Amount": 1.45}], "Note": "", "Number": "84786", "Date": "2024-08-10", "Type": "N"}	Z7C7007088		2024-08-10 16:59:25	t	2024-08-10 16:57:58.968868	f
25	REF025	N	840	{"Reference": "REF025", "Printer_Serial": "Z7C7007088", "Client": {"Vat": "V16442063", "Name": "WILMAN JOSE CAMACHO GONZALEZ 123456789 ABCDEFGH", "Address": "SANTA ISABEL SAN FRANCISCO CALLE 06 CARR 5 OESTE"}, "Products": [{"Code": "01", "Name": "ACETAMINOFEN+CAFEINA PASTILLA TABLETA 30MG ADULTO", "Quantity": 1, "Price": 1.45, "Tax": "G"}, {"Code": "02", "Name": "ACETAMINOFEN+CAFEINA PASTILLA TABLETA 15MG ADULTO", "Quantity": 1, "Price": 1.45, "Tax": "E"}], "Payments": [{"Code": "01", "Name": "efectivo", "Amount": 1.68}, {"Code": "02", "Name": "debito", "Amount": 1.45}], "Note": "", "Number": "84787", "Date": "2024-08-10", "Type": "N"}	Z7C7007088		2024-08-10 16:59:58	t	2024-08-10 16:58:12.450031	f
26	REF026	N	841	{"Reference": "REF026", "Printer_Serial": "Z7C7007088", "Client": {"Vat": "V16442063", "Name": "WILMAN JOSE CAMACHO GONZALEZ 123456789 ABCDEFGH", "Address": "SANTA ISABEL SAN FRANCISCO CALLE 06 CARR 5 OESTE"}, "Products": [{"Code": "01", "Name": "ACETAMINOFEN+CAFEINA PASTILLA TABLETA 30MG ADULTO", "Quantity": 1, "Price": 1.45, "Tax": "G"}, {"Code": "02", "Name": "ACETAMINOFEN+CAFEINA PASTILLA TABLETA 15MG ADULTO", "Quantity": 1, "Price": 1.45, "Tax": "E"}], "Payments": [{"Code": "01", "Name": "efectivo", "Amount": 1.68}, {"Code": "02", "Name": "debito", "Amount": 1.45}], "Note": "", "Number": "84788", "Date": "2024-08-10", "Type": "N"}	Z7C7007088		2024-08-10 17:00:06	t	2024-08-10 16:58:23.586354	f
28	REF0028	X		{"Reference": "REF0028", "Printer_Serial": "Z7C7007088", "Type": "X"}	Z7C7007088		2024-08-10 17:01:57	t	2024-08-10 17:01:47.739329	f
20	REF020	F	84787	{"Reference": "REF020", "Printer_Serial": "Z7C7007088", "Client": {"Vat": "V16442063", "Name": "WILMAN JOSE CAMACHO GONZALEZ 123456789 ABCDEFGH", "Address": "SANTA ISABEL SAN FRANCISCO CALLE 06 CARR 5 OESTE"}, "Products": [{"Code": "01", "Name": "ACETAMINOFEN+CAFEINA PASTILLA TABLETA 30MG ADULTO", "Quantity": 1, "Price": 1.45, "Tax": "G"}, {"Code": "02", "Name": "ACETAMINOFEN+CAFEINA PASTILLA TABLETA 15MG ADULTO", "Quantity": 1, "Price": 1.45, "Tax": "E"}], "Payments": [{"Code": "01", "Name": "efectivo", "Amount": 1.68}, {"Code": "02", "Name": "debito", "Amount": 1.45}], "Note": "", "Number": "", "Date": "", "Type": "F"}	Z7C7007088		2024-08-10 16:55:20	t	2024-08-10 16:52:36.237754	f
30	ref30	N	\N	{"Reference": "ref30", "Printer_Serial": "Z7C7007088", "Client": {"Vat": "16442063", "Name": "Wilman Camacho", "Address": "bqto"}, "Products": [{"Code": "01", "Name": "producto 1", "Quantity": 1, "Price": 5.0, "Tax": "G"}], "Payments": [{"Code": "01", "Name": "efectivo", "Amount": 5.8}], "Note": "", "Number": "84794", "Date": "2024-08-10", "Discount": 10.5, "Type": "N"}	Z7C7007088	\N	\N	t	2024-08-10 18:56:20.813849	f
29	REF0029	Z	887	{"Reference": "REF0029", "Printer_Serial": "Z7C7007088", "Type": "Z"}	Z7C7007088		2024-08-10 17:02:47	t	2024-08-10 17:01:57.879926	f
27	REF027	N	666	{"Reference": "REF027", "Printer_Serial": "Z7C7007088", "Client": {"Vat": "V16442063", "Name": "WILMAN JOSE CAMACHO GONZALEZ 123456789 ABCDEFGH", "Address": "SANTA ISABEL SAN FRANCISCO CALLE 06 CARR 5 OESTE"}, "Products": [{"Code": "01", "Name": "ACETAMINOFEN+CAFEINA PASTILLA TABLETA 30MG ADULTO", "Quantity": 1, "Price": 1.45, "Tax": "G"}, {"Code": "02", "Name": "ACETAMINOFEN+CAFEINA PASTILLA TABLETA 15MG ADULTO", "Quantity": 1, "Price": 1.45, "Tax": "E"}], "Payments": [{"Code": "01", "Name": "efectivo", "Amount": 1.68}, {"Code": "02", "Name": "debito", "Amount": 1.45}], "Note": "", "Number": "84789", "Date": "2024-08-10", "Type": "N"}	Z7C7007088	P	2024-08-13 23:37:10	t	2024-08-10 16:58:36.633551	f
21	REF021	F	84788	{"Reference": "REF021", "Printer_Serial": "Z7C7007088", "Client": {"Vat": "V16442063", "Name": "WILMAN JOSE CAMACHO GONZALEZ 123456789 ABCDEFGH", "Address": "SANTA ISABEL SAN FRANCISCO CALLE 06 CARR 5 OESTE"}, "Products": [{"Code": "01", "Name": "ACETAMINOFEN+CAFEINA PASTILLA TABLETA 30MG ADULTO", "Quantity": 1, "Price": 1.45, "Tax": "G"}, {"Code": "02", "Name": "ACETAMINOFEN+CAFEINA PASTILLA TABLETA 15MG ADULTO", "Quantity": 1, "Price": 1.45, "Tax": "E"}], "Payments": [{"Code": "01", "Name": "efectivo", "Amount": 1.68}, {"Code": "02", "Name": "debito", "Amount": 1.45}], "Note": "", "Number": "", "Date": "", "Type": "F"}	Z7C7007088		2024-08-10 16:55:50	t	2024-08-10 16:52:42.280125	f
18	REF018	T	REF018	{\n  "Reference": "REF018",\n  "Printer_Serial": "Z7C7007088",\n  "Client": {\n    "Vat": "V16442063",\n    "Name": "WILMAN JOSE CAMACHO GONZALEZ 123456789 ABCDEFGH",\n    "Address": "SANTA ISABEL SAN FRANCISCO CALLE 06 CARR 5 OESTE"\n  },\n  "Products": [\n    {\n      "Code": "01",\n      "Name": "ACETAMINOFEN+CAFEINA PASTILLA TABLETA 30MG ADULTO",\n      "Quantity": 1,\n      "Price": 20.45,\n      "Tax": "G"\n    },\n    {\n      "Code": "02",\n      "Name": "ACETAMINOFEN+CAFEINA PASTILLA TABLETA 15MG ADULTO",\n      "Quantity": 1,\n      "Price": 165.45,\n      "Tax": "E"\n    }\n  ],\n  "Payments": [\n    {\n      "Code": "01",\n      "Name": "efectivo",\n      "Amount": 1.68\n    },\n    {\n      "Code": "02",\n      "Name": "debito",\n      "Amount": 1.45\n    }\n  ],\n  "Note": "",\n  "Number": "",\n  "Date": "",\n  "Type": "T"\n}	POS-80-Series	P	2024-08-17 14:36:03	t	2024-08-10 16:52:26.652387	f
31	REF030	T	REF030	{"Reference": "REF030", "Printer_Serial": "POS-80-Series", "Client": {"Vat": "V16442063", "Name": "WILMAN JOSE CAMACHO GONZALEZ 123456789 ABCDEFGH", "Address": "SANTA ISABEL SAN FRANCISCO CALLE 06 CARR 5 OESTE"}, "Products": [{"Code": "01", "Name": "ACETAMINOFEN+CAFEINA PASTILLA TABLETA 30MG ADULTO", "Quantity": 1, "Price": 20.45, "Tax": "G"}, {"Code": "02", "Name": "ACETAMINOFEN+CAFEINA PASTILLA TABLETA 15MG ADULTO", "Quantity": 1, "Price": 165.45, "Tax": "E"}], "Payments": [{"Code": "01", "Name": "efectivo", "Amount": 1.68}, {"Code": "02", "Name": "debito", "Amount": 1.45}], "Notes": [{"Description": "SUBTOTAL", "Value": "10.22"}, {"Description": "IVA", "Value": "3.12"}, {"Description": "TOTAL", "Value": "13.33"}], "Number": "", "Date": "", "Discount": 0.0, "Type": "T"}	POS-80-Series	P	2024-08-17 15:41:03	t	2024-08-17 15:23:01.95306	f
32	REF030	F	84807	{\n  "Reference": "REF030",\n  "Printer_Serial": "Z7C7007088",\n  "Client": {\n    "Vat": "V16442063",\n    "Name": "WILMAN JOSE CAMACHO GONZALEZ 123456789 ABCDEFGH",\n    "Address": "SANTA ISABEL SAN FRANCISCO CALLE 06 CARR 5 OESTE"\n  },\n  "Products": [\n    {\n      "Code": "01",\n      "Name": "ACETAMINOFEN+CAFEINA PASTILLA TABLETA 30MG ADULTO",\n      "Quantity": 1,\n      "Price": 1.45,\n      "Tax": "G"\n    },\n    {\n      "Code": "02",\n      "Name": "ACETAMINOFEN+CAFEINA PASTILLA TABLETA 15MG ADULTO",\n      "Quantity": 1,\n      "Price": 2.3,\n      "Tax": "E"\n    }\n  ],\n  "Payments": [\n    {\n      "Code": "01",\n      "Name": "efectivo",\n      "Amount": 1.68\n    },\n    {\n      "Code": "02",\n      "Name": "debito",\n      "Amount": 2.3\n    }\n  ],\n  "Notes": [\n    {\n      "text": "Gracias por su Compra!..."\n    },\n    {\n      "text": "Vuelva Pronto!..."\n    },\n    {\n      "text": "FSI"\n    }\n  ],\n  "BillNumber": "",\n  "BillDate": "",\n  "Discount": 0.0,\n  "Type": "F"\n}	Z7C7007088	P	2024-08-17 18:42:06	t	2024-08-17 15:54:27.593686	f
33	8899	T	8899	{"Reference": "8899", "Printer_Serial": "PrintPDF", "Text": "            FRESCONI C.A.             \\n\\n============ REIMPRESION =============\\n\\n                ZULIA                 \\nRIF:J0409399109   Afil:J0409399109001 \\n             VENTA DEBITO             \\n         589524*****9679          \\nBANCO PROVINCIAL         J-000029679  \\nFecha: 23/05/2024  Hora: 04:29:27 PM\\n S/N POS   Nr Autor.  Nr Operac.    \\n 11648602  242362     064154\\n Terminal 03   Lote 126   Ticket 0242\\nMONTO       Bs.                   1,00\\n          NO REQUIERE FIRMA           \\nMaestro\\nAID:A0000000043060 CT:FBB9D611DEB0AF50\\nVersion 6.0 - Platco 40\\n                               (SiTef)\\n\\n============ REIMPRESION =============\\n\\n"}	PrintPDF	P	2024-09-08 22:08:51	t	2024-09-08 14:06:58.569362	f
\.


--
-- TOC entry 4794 (class 0 OID 0)
-- Dependencies: 215
-- Name: line_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.line_id_seq', 33, true);


--
-- TOC entry 4642 (class 2606 OID 16407)
-- Name: line line_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.line
    ADD CONSTRAINT line_pkey PRIMARY KEY (id);


-- Completed on 2024-12-08 08:25:06

--
-- PostgreSQL database dump complete
--

-- Completed on 2024-12-08 08:25:06

--
-- PostgreSQL database cluster dump complete
--

