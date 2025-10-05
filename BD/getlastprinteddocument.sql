CREATE OR REPLACE FUNCTION public.getlastprinteddocument(v_printer_serial character varying, v_document_type character varying)
 RETURNS json
 LANGUAGE plpgsql
AS $function$
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
	$function$
;
