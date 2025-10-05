CREATE OR REPLACE FUNCTION public.getstatus(
v_document_reference character varying, 
v_printer_serial character varying,
v_document_type character varying)
 RETURNS json
 LANGUAGE plpgsql
AS $function$
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
$function$
;
