
CREATE OR REPLACE FUNCTION public.getlinejson(v_printer_serial character varying)
 RETURNS json
 LANGUAGE plpgsql
AS $function$
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
	$function$
;
