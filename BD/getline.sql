CREATE OR REPLACE FUNCTION public.getline(v_printer_serial character varying)
 RETURNS TABLE(id integer, document_reference character varying, printer_serial character varying, document_json text, printed_note character varying, document_number character varying, document_type integer, reprint boolean)
 LANGUAGE plpgsql
AS $function$
 
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
$function$
;
